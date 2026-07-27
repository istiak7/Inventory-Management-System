namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    // Payment taken at sale time. Mode is inferred from Amount vs the server-recalculated total:
    //   Amount omitted / 0  -> Credit (nothing paid, full amount due)
    //   0 < Amount < Total   -> Credit (partial)
    //   Amount == Total      -> Cash   (full)
    //   Amount > Total        -> rejected
    public class SalePaymentRequest
    {
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank Transfer, Cheque, Credit Card, Other
    }
}
