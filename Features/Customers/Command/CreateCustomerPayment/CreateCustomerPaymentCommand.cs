using Inventory_Management_System.Features.Customers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Command.CreateCustomerPayment
{
    public class CreateCustomerPaymentCommand : IRequest<Result>
    {
        public int CustomerId { get; set; }
        public int BranchId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? Remarks { get; set; }

        public List<SaleAllocationRequest> Allocations { get; set; } = [];
    }
}
