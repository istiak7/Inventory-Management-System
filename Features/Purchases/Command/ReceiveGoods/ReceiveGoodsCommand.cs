using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.ReceiveGoods
{
    public class ReceiveGoodsCommand : IRequest<Result>
    {
        public int PurchaseOrderId { get; set; }
        public List<ReceiveLineRequest> Lines { get; set; } = [];

        // True = the supplier will not send the rest. After this receipt the order is closed:
        // lines are cut down to what was really received, and the supplier is owed only for that.
        public bool CloseRemaining { get; set; }

        // How the order is settled: "Cash" or "Debit". Chosen here, at approval, not when the
        // order was raised. Required once this receipt brings every line in.
        public string? PaymentType { get; set; }

        // Only for a Debit approval: how much of the order is paid up front. Left null or 0,
        // the whole amount stays on the supplier account.
        public decimal? PaymentAmount { get; set; }

        // Date to record the settlement on; defaults to now.
        public DateTime? PaymentDate { get; set; }
    }
}
