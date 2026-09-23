using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Purchases.Command.DeletePurchaseOrder
{
    public class DeletePurchaseOrderHandler(
        AppDbContext _dbContext,
        ILogger<DeletePurchaseOrderHandler> _logger
    ) : IRequestHandler<DeletePurchaseOrderCommand, Result>
    {
        public async Task<Result> Handle(DeletePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            // Lock the order so goods cannot be received while the order is being deleted.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.LockRowAsync<SupplierPurchase>(request.PurchaseOrderId, cancellationToken);

            var purchase = await _dbContext.SupplierPurchases
                .Include(p => p.SupplierPurchaseDetails)
                .FirstOrDefaultAsync(
                    p => p.Id == request.PurchaseOrderId && p.IsActive != (int)EntityStatus.Deleted,
                    cancellationToken);

            if (purchase == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Purchase order not found."
                };

            // A completed order has stock and supplier ledger entries behind it — removing it
            // would leave both wrong, so it stays for good.
            if (purchase.IsCompleted)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "This purchase order is completed and can no longer be deleted."
                };

            if (purchase.Status == PurchaseStatus.PartiallyReceived)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Goods have already been received against this purchase order, so it can no longer be deleted."
                };

            try
            {
                // Soft delete, the same way the rest of the system retires a record.
                purchase.Delete();
                purchase.UpDatedAt = DateTime.Now;
                foreach (var detail in purchase.SupplierPurchaseDetails)
                {
                    detail.Delete();
                    detail.UpDatedAt = DateTime.Now;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Purchase order deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting purchase order {Id}", request.PurchaseOrderId);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while deleting the purchase order."
                };
            }
        }
    }
}
