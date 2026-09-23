using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Inventory_Management_System.Shared.CurrentUser;

namespace Inventory_Management_System.Features.Suppliers.Command.CreateSupplierPayment
{
    public class CreateSupplierPaymentCommand : IRequest<Result>, IBranchScopedRequest
    {
        public bool IsAllowedForBranch(int userBranchId) => BranchId == userBranchId;

        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? Remarks { get; set; }

        public List<PaymentAllocationRequest> Allocations { get; set; } = [];
    }
}
