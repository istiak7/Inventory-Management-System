using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
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
            var purchase = await _dbContext.SupplierPurchases
                .Include(p => p.SupplierPurchaseDetails)
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId, cancellationToken);

            if (purchase == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Purchase order not found." };

            if (purchase.Status != "Pending")
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Only a pending purchase order can be rejected." };

            try
            {
                // Reject is purely a status change — no ledger entry, no payment, no stock impact.
                purchase.Status = "Rejected";
                foreach (var detail in purchase.SupplierPurchaseDetails)
                    detail.IsApproved = "Rejected";

                await _dbContext.SaveChangesAsync(cancellationToken);

                var response = new PurchaseOrderResponse(
                    purchase.Id, purchase.SupplierId, purchase.BranchId, purchase.PurchaseDate,
                    purchase.InvoiceNumber, purchase.Status, purchase.PurchaseType,
                    purchase.TotalAmount, purchase.DueAmount, null);

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
