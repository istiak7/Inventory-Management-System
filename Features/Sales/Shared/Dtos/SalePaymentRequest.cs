namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    // What the caller can say about a settlement. A Cash sale always clears in full, so only
    // the date applies to it. A Debit sale may carry a partial payment taken at the counter,
    // and Amount is how much of the total that is.
    public class SalePaymentRequest
    {
        public decimal? Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
