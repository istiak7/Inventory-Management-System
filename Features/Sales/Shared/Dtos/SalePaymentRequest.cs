namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public class SalePaymentRequest
    {
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }
}
