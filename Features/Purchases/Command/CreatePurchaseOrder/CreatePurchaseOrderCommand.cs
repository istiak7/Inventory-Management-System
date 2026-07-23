using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.CreatePurchaseOrder
{
    public class CreatePurchaseOrderCommand : IRequest<Result>
    {
        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public List<PurchaseItemRequest> Items { get; set; } = [];
        public PaymentRequest? Payment { get; set; }
    }
}
