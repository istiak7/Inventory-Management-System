using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

            if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
            {
                var taken = await _dbContext.SupplierPurchases
                    .AnyAsync(p => p.InvoiceNumber == request.InvoiceNumber, cancellationToken);
                if (taken)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Invoice number '{request.InvoiceNumber}' already exists."
                    };
            }

            try
            {
                var purchaseDate = request.PurchaseDate ?? DateTime.Now;

                var invoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber)
                    ? await GenerateInvoiceNumberAsync(purchaseDate, cancellationToken)
                    : request.InvoiceNumber.Trim();

                var purchase = new SupplierPurchase
                {
                    SupplierId = request.SupplierId,
                    BranchId = request.BranchId,
                    PurchaseDate = purchaseDate,
                    InvoiceNumber = invoiceNumber,
                    Status = PurchaseStatus.Pending,
                    PurchaseType = PurchaseType.Credit,
                    Remarks = request.Remarks,
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
                        ReceivedQuantity = null,
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
                    purchase.Remarks,
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
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                var constraint = (ex.InnerException as PostgresException)?.ConstraintName;
                _logger.LogWarning(ex, "Purchase order creation lost a uniqueness race on {Constraint}", constraint);

                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 409,
                    Status = "Error",
                    Message = "That invoice number was taken by another purchase order a moment ago. Leave it blank to have one generated."
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

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

        private async Task<string> GenerateInvoiceNumberAsync(
            DateTime purchaseDate,
            CancellationToken cancellationToken
        )
        {
            var prefix = $"PO-{purchaseDate.Year}-";

            var latest = await _dbContext.SupplierPurchases
                .AsNoTracking()
                .Where(p => p.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(p => p.Id)
                .Select(p => p.InvoiceNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var next = 1;
            if (latest != null && int.TryParse(latest[prefix.Length..], out var lastSequence))
                next = lastSequence + 1;

            return prefix + next.ToString("D4");
        }
    }
}
