namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public class PaymentAllocationRequest
    {
        public int PurchaseId { get; set; }
        public decimal Amount { get; set; }
        /// <summary>Optional discount waived on this invoice together with the payment.</summary>
        public decimal Discount { get; set; }
    }
}
