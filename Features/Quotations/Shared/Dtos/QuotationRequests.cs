namespace Inventory_Management_System.Features.Quotations.Shared.Dtos
{
    // Body of create-quotation and update-quotation. The quotation number is always generated.
    public class SaveQuotationRequest
    {
        public int CustomerId { get; set; }
        // Date-only values ("2026-11-10"). QuotationDate defaults to today.
        public DateTime? QuotationDate { get; set; }
        public DateTime ValidUntil { get; set; }
        public List<QuotationItemRequest> Items { get; set; } = [];
        // Extra discount on the whole quotation, after the line discounts.
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string? Notes { get; set; }
        public string? TermsAndConditions { get; set; }
    }

    public class QuotationItemRequest
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        // Omit to quote the variant's current selling price. Stored as a snapshot either way.
        public decimal? UnitPrice { get; set; }
        // Discount on this line, as an amount.
        public decimal DiscountAmount { get; set; }
        // Unit of measurement, e.g. "Pcs". Defaults to "Pcs".
        public string? Uom { get; set; }
        // Warranty text printed on the quotation, e.g. "3 Years". Optional.
        public string? Warranty { get; set; }
    }

    public class ChangeQuotationStatusRequest
    {
        // Sent, Accepted, Rejected, Expired or Draft (re-open). Converted is set only by conversion.
        public string Status { get; set; } = string.Empty;
    }
}
