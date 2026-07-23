namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public class PaymentAllocationRequest
    {
        public int PurchaseId { get; set; }
        public decimal Amount { get; set; }
    }
}
