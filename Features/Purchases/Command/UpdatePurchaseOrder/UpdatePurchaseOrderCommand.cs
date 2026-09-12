using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.UpdatePurchaseOrder
{
    public class UpdatePurchaseOrderCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Remarks { get; set; }
        public List<PurchaseItemRequest> Items { get; set; } = [];
    }
}
