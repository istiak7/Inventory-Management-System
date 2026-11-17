using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.EditPurchaseOrder
{
    // One line of the edited bill. PurchaseDetailsId names an existing line (keep / change it);
    // a line without one is new. An existing line that is not listed is removed from the bill.
    public class EditPurchaseItemRequest
    {
        public int? PurchaseDetailsId { get; set; }
        public int ProductVariantId { get; set; }
        // For a serialized line this must equal the number of SerialNumbers.
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int WarrantyDays { get; set; }
        // Serialized lines only: EVERY serial the line should have after the edit, including the
        // ones it already has. A serial that is left out is removed (only possible while it is
        // still in stock and has no history); a new one is added to stock.
        public List<string>? SerialNumbers { get; set; }
    }

    // Edits an approved purchase. The warehouse stays as it is; the supplier, Cash / Credit type,
    // invoice number, date, lines, costs, discount and remarks can change.
    public class EditPurchaseOrderCommand : IRequest<Result>
    {
        public int Id { get; set; }
        // Null / blank keeps the current value. The invoice number must stay unique.
        public string? InvoiceNumber { get; set; }
        public DateTime? PurchaseDate { get; set; }
        // Null keeps the current supplier. A different one moves the bill: it comes off the old
        // supplier's account (what we paid them on it stays our advance with them, or is marked as
        // paid back when RefundExcess is set) and is booked in full to the new supplier. Not
        // allowed once anything has been returned on the bill.
        public int? SupplierId { get; set; }
        // "Cash" or "Debit"; null keeps the current one. Not allowed once anything was returned.
        public string? PaymentType { get; set; }
        public List<EditPurchaseItemRequest> Items { get; set; } = [];
        public decimal DiscountAmount { get; set; }
        public string? Remarks { get; set; }
        // Why the bill is being changed. Kept in the edit history.
        public string Reason { get; set; } = string.Empty;
        // Money paid to the supplier now. A Cash purchase is settled in full, so it has to equal
        // what the edit adds to the bill (the amount is told in the error when it is missing); a
        // Credit purchase may pay part of what is due.
        public decimal? AdditionalPayment { get; set; }
        // When the edit leaves us overpaid: true records that the supplier paid the surplus back in
        // cash (a refund); false (default) keeps it as our advance with the supplier.
        public bool RefundExcess { get; set; }
    }
}
