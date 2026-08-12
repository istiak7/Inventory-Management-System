namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public class CreateSupplierPaymentRequest
    {
        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank, bKash, Nagad, Rocket, Cheque
        public string? Remarks { get; set; }

        // Optional per-invoice allocation. When provided, the payment is applied to exactly these
        // invoices. When omitted/empty, the server auto-allocates FIFO (oldest invoices first).
        public List<PaymentAllocationRequest>? Allocations { get; set; }
    }
}
