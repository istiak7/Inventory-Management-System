using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.ApprovePurchaseOrder
{
    public class ApprovePurchaseOrderCommand : IRequest<Result>
    {
        public int PurchaseOrderId { get; set; }

        // Optional — omitted/null = approve on account (full due), no payment posted.
        public PaymentRequest? Payment { get; set; }
    }
}
