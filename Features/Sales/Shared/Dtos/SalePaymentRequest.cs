namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    // What the caller can still say about a settlement. How much is settled is decided by the
    // sale's PaymentType, not by the caller, so only the date is left.
    public class SalePaymentRequest
    {
        public DateTime? PaymentDate { get; set; }
    }
}
