using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Purchases.Command.CreatePurchaseOrder
{
    public class CreatePurchaseOrderHandler(
            AppDbContext _dbContext,
            ILogger<CreatePurchaseOrderHandler> _logger
        ) : IRequestHandler<CreatePurchaseOrderCommand, Result>
    {
        public async Task<Result> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.Items.Count == 0)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "At least one purchase item is required." };

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                {
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = "Quantity must be positive."
                    };
                }
                if (item.UnitPrice < 0)
                {
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = "Unit price cannot be negative."
                    };
                }
            }

            var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
            if (supplier == null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Supplier not found."
                };
            }
            var branch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Branch not found."
                };
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {

                var purchase = new SupplierPurchase
                {
                    SupplierId = request.SupplierId,
                    BranchId = request.BranchId,
                    PurchaseDate = request.PurchaseDate ?? DateTime.Now,
                    InvoiceNumber = request.InvoiceNumber,
                    Status = "Pending",
                    Supplier = supplier,
                    Branch = branch,
                };

                // Build the line items — TotalAmount is the sum of every line total.
                decimal totalAmount = 0;
                foreach (var item in request.Items)
                {
                    var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);
                    if (product == null)
                    {
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 404,
                            Status = "Error",
                            Message = $"Product with id {item.ProductId} not found."
                        };
                    }
                    var lineTotal = item.Quantity * item.UnitPrice;
                    totalAmount += lineTotal;

                    purchase.SupplierPurchaseDetails.Add(new SupplierPurchaseDetails
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalAmount = lineTotal,
                        IsApproved = "Pending",
                        SupplierPurchase = purchase,
                        Product = product,
                    });
                }

                // Header holds the money: nothing is paid until approval, so due == total.
                purchase.TotalAmount = totalAmount;
                purchase.PaidAmount = 0;
                purchase.DueAmount = totalAmount;


                var intendedAmount = request.Payment?.Amount ?? 0;
                purchase.PurchaseType = intendedAmount <= 0 ? "due"
                                      : intendedAmount >= totalAmount ? "fillpayment"
                                      : "partial";

                await _dbContext.SupplierPurchases.AddAsync(purchase, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new PurchaseOrderResponse(
                    purchase.Id, purchase.SupplierId, purchase.BranchId, purchase.PurchaseDate,
                    purchase.InvoiceNumber, purchase.Status, purchase.PurchaseType,
                    purchase.TotalAmount, purchase.DueAmount,
                    null
                );

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Purchase order created successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating purchase order");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the purchase order."
                };
            }
        }
    }
}
