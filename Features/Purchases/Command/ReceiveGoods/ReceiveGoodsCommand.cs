using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.ReceiveGoods
{
    public class ReceiveGoodsCommand : IRequest<Result>
    {
        public int PurchaseOrderId { get; set; }
        public List<ReceiveLineRequest> Lines { get; set; } = [];
    }
}
