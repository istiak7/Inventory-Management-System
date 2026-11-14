using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.CancelPurchaseOrder
{
    // Deletes (cancels) an APPROVED purchase. The bill is never removed: it becomes Cancelled, the
    // stock it still holds is taken back out and what we owe on it is reversed. (A pending order
    // that never received anything is deleted with the normal Delete.)
    public class CancelPurchaseOrderCommand : IRequest<Result>
    {
        public int Id { get; set; }
        // Why the bill is cancelled. Kept in the bill's history.
        public string Reason { get; set; } = string.Empty;
        // Money we paid the supplier stays on their account as our advance. True records that the
        // supplier also paid it back in cash (a refund); false (default) never creates a refund.
        public bool RefundCash { get; set; }
    }
}
