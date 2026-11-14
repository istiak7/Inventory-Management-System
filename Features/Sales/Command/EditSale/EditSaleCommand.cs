using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Command.EditSale
{
    // One line of the edited invoice. SaleDetailsId names an existing line (keep / change it);
    // a line without one is new. An existing line that is not listed is removed from the invoice.
    public class EditSaleItemRequest
    {
        public int? SaleDetailsId { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        // Null keeps the line's price (existing line) or uses the product's price (new line).
        public decimal? UnitPrice { get; set; }
        public int? WarrantyDays { get; set; }
        public string? SerialNumber { get; set; }
    }

    // Edits a completed sale. The customer, branch, invoice number, date and Cash / Credit type
    // stay as they are; lines, prices, discount, tax and remarks can change.
    public class EditSaleCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public List<EditSaleItemRequest> Items { get; set; } = [];
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string? Remarks { get; set; }
        // Why the invoice is being changed. Kept in the edit history.
        public string Reason { get; set; } = string.Empty;
        // Money collected now. A Cash sale must be settled in full, so it has to equal what the
        // edit adds to the invoice (the amount is told in the error when it is missing); a Credit
        // sale may take part of what is due.
        public decimal? AdditionalPayment { get; set; }
        // When the edit leaves the customer overpaid: true hands that money back in cash (recorded
        // as a refund); false (default) keeps it as the customer's advance.
        public bool RefundExcess { get; set; }
    }
}
