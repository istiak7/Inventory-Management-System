using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // A price offer to a customer. It is only a document: making, changing or deleting a
    // quotation never moves stock and never writes to the customer ledger. Those happen only
    // when an Accepted quotation is converted into a sale (see CreateSaleHandler).
    public class Quotation : BaseEntity
    {
        public required string QuotationNumber { get; set; }
        public int CustomerId { get; set; }
        public DateTime QuotationDate { get; set; } = DateTime.UtcNow;
        // Last day the offer stands (a shop calendar day, stored like other date-only inputs).
        public DateTime ValidUntil { get; set; }
        public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

        // SubTotal = sum of the lines after their own discounts.
        // TotalAmount = SubTotal - DiscountAmount (extra discount on the whole quotation) + TaxAmount.
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }
        public string? TermsAndConditions { get; set; }

        // Set once, when the quotation becomes a sale. The unique index on ConvertedSaleId
        // and the row lock in CreateSaleHandler stop it from being converted twice.
        public int? ConvertedSaleId { get; set; }
        public DateTime? ConvertedAt { get; set; }

        // Navigation property
        public required Customer Customer { get; set; }
        public CustomerSale? ConvertedSale { get; set; }
        public ICollection<QuotationDetails> QuotationDetails { get; set; } = [];
    }

    public class QuotationDetails : BaseEntity
    {
        public int QuotationId { get; set; }
        public int ProductVariantId { get; set; }

        // Snapshot of the product as quoted, so a later rename or price change does not
        // rewrite a quotation the customer already has.
        public required string ProductName { get; set; }
        public required string Sku { get; set; }
        // Unit of measurement printed on the quotation, e.g. "Pcs", "Box", "Set".
        public string Uom { get; set; } = "Pcs";

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        // Discount on this line, as an amount. TotalAmount = Quantity * UnitPrice - DiscountAmount.
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // Navigation property
        public required Quotation Quotation { get; set; }
        public required ProductVariant ProductVariant { get; set; }
    }
}
