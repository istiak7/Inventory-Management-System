namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public class CreateCustomerPaymentRequest
    {
        public int CustomerId { get; set; }
        public int BranchId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? Remarks { get; set; }

        public List<SaleAllocationRequest>? Allocations { get; set; }
    }
}
