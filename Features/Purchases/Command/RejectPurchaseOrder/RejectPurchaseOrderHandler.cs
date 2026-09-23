using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using static Inventory_Management_System.Entities.Common.EntityConstant;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Purchases.Command.RejectPurchaseOrder
{
    public class RejectPurchaseOrderHandler(
        AppDbContext _dbContext,
        ILogger<RejectPurchaseOrderHandler> _logger
    ) : IRequestHandler<RejectPurchaseOrderCommand, Result>
    {
        public async Task<Result> Handle(RejectPurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            // Lock the order so goods cannot be received while the order is being rejected.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.LockRowAsync<SupplierPurchase>(request.PurchaseOrderId, cancellationToken);

            var purchase = await _dbContext.SupplierPurchases
                .Include(p => p.SupplierPurchaseDetails)
                .FirstOrDefaultAsync(
                    p => p.Id == request.PurchaseOrderId && p.IsActive != (int)EntityStatus.Deleted,
                    cancellationToken);

            if (purchase == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Purchase order not found." };

            if (purchase.Status != PurchaseStatus.Pending)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Only a pending purchase order can be rejected." };

            try
            {
                purchase.Status = PurchaseStatus.Rejected;
                // Nothing will be delivered, so nothing is owed.
                purchase.DueAmount = 0;
                foreach (var detail in purchase.SupplierPurchaseDetails)
                    detail.Reject();

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new PurchaseOrderResponse(
                    purchase.Id, purchase.SupplierId, purchase.BranchId, purchase.PurchaseDate,
                    purchase.InvoiceNumber, purchase.Remarks, purchase.Status.ToString(), purchase.PurchaseType?.ToString(),
                    purchase.TotalAmount, purchase.DueAmount);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Purchase order rejected successfully", Data = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting purchase order");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while rejecting the purchase order." };
            }
        }
    }
}
