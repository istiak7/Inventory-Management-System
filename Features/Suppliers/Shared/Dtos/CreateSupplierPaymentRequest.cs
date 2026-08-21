namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public class CreateSupplierPaymentRequest
    {
        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? Remarks { get; set; }

        public List<PaymentAllocationRequest>? Allocations { get; set; }
    }
}
