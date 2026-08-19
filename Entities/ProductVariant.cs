using NpgsqlTypes;
using System.Text.Json;

namespace Inventory_Management_System.Entities
{
    public class ProductVariant : BaseEntity
    {
        public int ProductId { get; set; } 
        public required string SKU { get; set; }             
        public string Barcode { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }             
        public bool IsSerialized { get; set; } = false;
        public string AttributesJson { get; set; } = "{}";   
        public string SearchText { get; set; } = string.Empty;                                                   
        public NpgsqlTsVector SearchVector { get; set; } = default!;

        // Navigation property
        public required Product Product { get; set; }
        public ICollection<Stock> Stocks { get; set; } = [];
        public ICollection<SupplierPurchaseDetails> SupplierPurchaseDetails { get; set; } = [];
        public ICollection<SaleDetails> SaleDetails { get; set; } = [];
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
        public ICollection<ProductSerial> ProductSerials { get; set; } = [];

        private const int MaxSearchTextLength = 8000;

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
