using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Command.CancelSale
{
    // Deletes (cancels) a completed sale. The invoice is never removed: it becomes Cancelled and
    // everything it still does to stock, ledgers and settlements is reversed.
    public class CancelSaleCommand : IRequest<Result>
    {
        public int Id { get; set; }
        // Why the invoice is cancelled. Kept in the invoice's history.
        public string Reason { get; set; } = string.Empty;
        // Money the customer paid stays on their account as an advance. True records that it was
        // also handed back in cash (a refund); false (default) never creates a refund.
        public bool RefundCash { get; set; }
    }
}
