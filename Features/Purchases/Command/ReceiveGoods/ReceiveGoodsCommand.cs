using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.ReceiveGoods
{
    public class ReceiveGoodsCommand : IRequest<Result>
    {
        public int PurchaseOrderId { get; set; }
        public List<ReceiveLineRequest> Lines { get; set; } = [];

        // Date to record the cash settlement on. Only used when the order's payment type is
        // Cash and this receipt completes it; defaults to now.
        public DateTime? PaymentDate { get; set; }
    }
}
