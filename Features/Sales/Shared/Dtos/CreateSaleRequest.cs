namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public class CreateSaleRequest
    {
        // Exactly one of CustomerId / Customer identifies who this sale is for: an id when the
        // sales form matched the typed mobile number to somebody on file, a Customer block when
        // that lookup came back empty and the till is registering them now.
        public int? CustomerId { get; set; }
        public SaleCustomerRequest? Customer { get; set; }

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
