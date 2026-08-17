namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public class CreateCustomerPaymentRequest
    {
        public int CustomerId { get; set; }
        public int BranchId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank, bKash, Nagad, Rocket, Cheque
        public string? Remarks { get; set; }

        // Optional per-invoice allocation. When provided, the payment is applied to exactly these
        // sales. When omitted/empty, it is recorded as on-account credit (no auto-allocation).
        public List<SaleAllocationRequest>? Allocations { get; set; }
    }
}
