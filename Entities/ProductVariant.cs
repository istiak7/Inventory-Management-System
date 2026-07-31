using NpgsqlTypes;
using System.Text.Json;

namespace Inventory_Management_System.Entities
{
    // The actual purchasable / sellable / stockable unit. Purchasing, stock, serials and
    // inventory always reference ProductVariantId — never ProductId. Cost and warranty are
    // NEVER stored here; they live per purchase line ("lot"), since the same variant can be
    // bought at different costs/terms over time.
    public class ProductVariant : BaseEntity
    {
        public int ProductId { get; set; } //FK
        public required string SKU { get; set; }              // globally unique (unique index)
        public string Barcode { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }             // catalog price, independent of purchase cost
        public bool IsSerialized { get; set; } = false;
        public string AttributesJson { get; set; } = "{}";    // jsonb (+ GIN index); valid JSON, defaults to "{}"
        public string SearchText { get; set; } = string.Empty; // FTS                                                            
        public NpgsqlTsVector SearchVector { get; set; } = default!; // processed tsvector (DB trigger populate)

        // Navigation properties
        public required Product Product { get; set; }
        public ICollection<Stock> Stocks { get; set; } = [];
        public ICollection<SupplierPurchaseDetails> SupplierPurchaseDetails { get; set; } = [];
        public ICollection<SaleDetails> SaleDetails { get; set; } = [];
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
        public ICollection<ProductSerial> ProductSerials { get; set; } = [];

        // Guard rail for to_tsvector: Postgres rejects a tsvector larger than 1 MB, and
        // AttributesJson is client-supplied, so a huge blob would otherwise fail the INSERT.
        private const int MaxSearchTextLength = 8000;

        /// <summary>
        /// Rebuilds the plain-text search projection. <paramref name="productName"/> lets the
        /// caller supply the name when the Product navigation is not loaded (the usual case for
        /// a variant fetched by id).
        /// </summary>
        public void RebuildSearchText(string? productName = null)
        {
            var name = productName ?? Product?.ProductName;

            var parts = new[] { name, SKU, Barcode, ExtractAttributeValues(AttributesJson) }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var text = string.Join(' ', parts).Trim();

            SearchText = text.Length > MaxSearchTextLength
                ? text[..MaxSearchTextLength]
                : text;
        }

        // AttributesJson is arbitrary jsonb ({"ram": 16, "color": "red"}), so it cannot be
        // deserialized into Dictionary<string, string>. Walk the document instead and skip
        // anything that has no useful text; a malformed blob must never break SaveChanges.
        private static string ExtractAttributeValues(string attributesJson)
        {
            if (string.IsNullOrWhiteSpace(attributesJson)) return string.Empty;

            try
            {
                using var document = JsonDocument.Parse(attributesJson);
                if (document.RootElement.ValueKind != JsonValueKind.Object) return string.Empty;

                var values = document.RootElement.EnumerateObject()
                    .Select(p => p.Value.ValueKind switch
                    {
                        JsonValueKind.String => p.Value.GetString(),
                        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => p.Value.ToString(),
                        _ => null
                    })
                    .Where(v => !string.IsNullOrWhiteSpace(v));

                return string.Join(' ', values);
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }
    }
}
