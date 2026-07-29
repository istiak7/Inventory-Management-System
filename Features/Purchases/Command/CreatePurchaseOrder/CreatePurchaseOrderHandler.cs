using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
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
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "At least one purchase item is required."
                };

            var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
            if (supplier == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Supplier not found."
                };

            var branch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Branch not found."
                };

            try
            {

                var purchase = new SupplierPurchase
                {
                    SupplierId = request.SupplierId,
                    BranchId = request.BranchId,
                    PurchaseDate = request.PurchaseDate ?? DateTime.Now,
                    InvoiceNumber = request.InvoiceNumber,
                    Status = PurchaseStatus.Pending,
                    PurchaseType = PurchaseType.Credit,
                    Supplier = supplier,
                    Branch = branch,
                };


                decimal totalAmount = 0;
                foreach (var item in request.Items)
                {
                    var variant = await _dbContext.ProductVariants.FirstOrDefaultAsync(v => v.Id == item.ProductVariantId, cancellationToken);
                    if (variant == null)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 404,
                            Status = "Error",
                            Message = $"Product variant with id {item.ProductVariantId} not found."
                        };

                    var lineTotal = item.Quantity * item.UnitPrice;
                    totalAmount += lineTotal;

                    purchase.SupplierPurchaseDetails.Add(new SupplierPurchaseDetails
                    {
                        ProductVariantId = item.ProductVariantId,
                        OrderedQuantity = item.Quantity,
                        ReceivedQuantity = null,          // unknown until goods receipt
                        UnitPrice = item.UnitPrice,
                        TotalAmount = lineTotal,
                        WarrantyMonths = item.WarrantyMonths,
                        Status = LineStatus.Pending,
                        SupplierPurchase = purchase,
                        ProductVariant = variant,
                    });
                }

                purchase.TotalAmount = totalAmount;
                purchase.PaidAmount = 0;
                purchase.DueAmount = totalAmount;

                await _dbContext.SupplierPurchases.AddAsync(purchase, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var response = new PurchaseOrderResponse(
                    purchase.Id,
                    purchase.SupplierId,
                    purchase.BranchId,
                    purchase.PurchaseDate,
                    purchase.InvoiceNumber,
                    purchase.Status.ToString(),
                    purchase.PurchaseType.ToString(),
                    purchase.TotalAmount,
                    purchase.DueAmount);

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
