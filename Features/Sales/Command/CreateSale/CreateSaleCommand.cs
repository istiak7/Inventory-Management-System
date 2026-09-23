using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Inventory_Management_System.Shared.CurrentUser;

namespace Inventory_Management_System.Features.Sales.Command.CreateSale
{
    public class CreateSaleCommand : IRequest<Result>, IBranchScopedRequest
    {
        public bool IsAllowedForBranch(int userBranchId) => BranchId == userBranchId;

        public int? CustomerId { get; set; }
        public SaleCustomerRequest? Customer { get; set; }

        public int BranchId { get; set; }
        public DateTime? SaleDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Remarks { get; set; }
        public List<SaleItemRequest> Items { get; set; } = [];
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string? PaymentType { get; set; }
        public SalePaymentRequest? Payment { get; set; }
    }
}
