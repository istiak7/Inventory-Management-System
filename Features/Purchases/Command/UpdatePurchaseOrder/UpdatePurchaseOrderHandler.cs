using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Purchases.Command.UpdatePurchaseOrder
{
    public class UpdatePurchaseOrderHandler(
        AppDbContext _dbContext,
        ILogger<UpdatePurchaseOrderHandler> _logger
    ) : IRequestHandler<UpdatePurchaseOrderCommand, Result>
    {
        public async Task<Result> Handle(UpdatePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            // Lock the order so goods cannot be received while the order is being edited.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.LockRowAsync<SupplierPurchase>(request.Id, cancellationToken);

            var purchase = await _dbContext.SupplierPurchases
                .Include(p => p.Supplier)
                .Include(p => p.Branch)
                .Include(p => p.SupplierPurchaseDetails)
                .FirstOrDefaultAsync(
                    p => p.Id == request.Id && p.IsActive != (int)EntityStatus.Deleted,
                    cancellationToken);

            if (purchase == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Purchase order not found."
                };

            // The business rule the frontend also mirrors: once approved, the order is closed.
            if (purchase.IsCompleted)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "This purchase order is completed and can no longer be edited."
                };

            if (purchase.Status == PurchaseStatus.Rejected)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "A rejected purchase order can no longer be edited."
                };

            // Goods already in stock were counted against these lines, so the order is no
            // longer safe to rewrite.
            if (purchase.Status == PurchaseStatus.PartiallyReceived)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Goods have already been received against this purchase order, so it can no longer be edited."
                };

            var supplier = purchase.SupplierId == request.SupplierId
                ? purchase.Supplier
                : await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
            if (supplier == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Supplier not found."
                };

            var branch = purchase.BranchId == request.BranchId
                ? purchase.Branch
                : await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Branch not found."
                };

            var invoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber)
                ? purchase.InvoiceNumber
                : request.InvoiceNumber.Trim();

            if (invoiceNumber != purchase.InvoiceNumber)
            {
                var taken = await _dbContext.SupplierPurchases
                    .AnyAsync(p => p.InvoiceNumber == invoiceNumber && p.Id != purchase.Id, cancellationToken);
                if (taken)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Invoice number {invoiceNumber} already exists."
                    };
            }

            try
            {
                purchase.SupplierId = request.SupplierId;
                purchase.BranchId = request.BranchId;
                purchase.Supplier = supplier;
                purchase.Branch = branch;
                purchase.PurchaseDate = request.PurchaseDate ?? purchase.PurchaseDate;
                purchase.InvoiceNumber = invoiceNumber;
                purchase.Remarks = request.Remarks;
                purchase.UpDatedAt = DateTime.Now;

                // Nothing has been received yet, so the old lines carry no stock or serials and
                // can simply be replaced by what was submitted.
                _dbContext.SupplierPurchaseDetails.RemoveRange(purchase.SupplierPurchaseDetails);
                purchase.SupplierPurchaseDetails.Clear();

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

                // Nothing is paid before approval, so the whole revised total is still due.
                purchase.TotalAmount = totalAmount;
                purchase.PaidAmount = 0;
                purchase.DueAmount = totalAmount;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new PurchaseOrderResponse(
                    purchase.Id,
                    purchase.SupplierId,
                    purchase.BranchId,
                    purchase.PurchaseDate,
                    purchase.InvoiceNumber,
                    purchase.Remarks,
                    purchase.Status.ToString(),
                    purchase.PurchaseType?.ToString(),
                    purchase.TotalAmount,
                    purchase.DueAmount);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Purchase order updated successfully",
                    Data = response
                };
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Purchase order {Id} update lost a uniqueness race", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 409,
                    Status = "Error",
                    Message = "That invoice number was taken by another purchase order a moment ago."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating purchase order {Id}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while updating the purchase order."
                };
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
