using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Inventory_Management_System.Shared.CurrentUser;

namespace Inventory_Management_System.Features.Purchases.Command.CreatePurchaseOrder
{
    public class CreatePurchaseOrderCommand : IRequest<Result>, IBranchScopedRequest
    {
        public bool IsAllowedForBranch(int userBranchId) => BranchId == userBranchId;

        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Remarks { get; set; } = null;
        public List<PurchaseItemRequest> Items { get; set; } = [];
    }
}
