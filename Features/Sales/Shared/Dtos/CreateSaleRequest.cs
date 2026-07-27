namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public class CreateSaleRequest
    {
        public int CustomerId { get; set; }
        public int BranchId { get; set; }
        public DateTime? SaleDate { get; set; }

        // Optional: server auto-generates INV-{year}-0001 style when omitted.
        public string? InvoiceNumber { get; set; }

        public List<SaleItemRequest> Items { get; set; } = [];

        public decimal DiscountAmount { get; set; }   // header-level discount
        public decimal TaxAmount { get; set; }        // header-level tax

        // Optional: null/omitted = fully due, Amount == total = cash, otherwise partial.
        public SalePaymentRequest? Payment { get; set; }
    }
}
