namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public class SaleAllocationRequest
    {
        public int SaleId { get; set; }
        public decimal Amount { get; set; }
        /// <summary>Optional discount waived on this invoice together with the payment.</summary>
        public decimal Discount { get; set; }
    }
}
