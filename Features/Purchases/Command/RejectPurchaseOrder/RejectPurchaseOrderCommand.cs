using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.RejectPurchaseOrder
{
    public class RejectPurchaseOrderCommand : IRequest<Result>
    {
        public int PurchaseOrderId { get; set; }
    }
}
