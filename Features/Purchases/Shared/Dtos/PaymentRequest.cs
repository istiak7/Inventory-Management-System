namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    // Payment taken at PO creation. Mode is inferred from Amount vs the server-recalculated total:
    //   Amount omitted / 0  -> Due   (nothing paid)
    //   0 < Amount < Total   -> Partial
    //   Amount == Total      -> Cash  (full)
    //   Amount > Total        -> rejected (use Cash instead)
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank Transfer, Cheque, Credit Card, Other
    }
}
