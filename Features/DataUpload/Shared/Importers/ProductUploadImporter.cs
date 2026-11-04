using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    /// <summary>
    /// One row = one variant (one SKU). Rows with the same Product Name + Sub Category + Brand are
    /// variants of one product. If that product already exists, the new variants are added to it.
    /// </summary>
    public class ProductUploadImporter(AppDbContext _db) : IUploadImporter
    {
        public static readonly UploadColumn ProductName = new(1, "Product Name", true, "Rows with the same name, sub-category and brand become one product.", 100);
        public static readonly UploadColumn SubCategoryCode = new(2, "Sub Category Code", true, "Code of an existing sub-category.", 50);
        public static readonly UploadColumn BrandName = new(3, "Brand Name", true, "Name of an existing brand.", 200);
        public static readonly UploadColumn Sku = new(4, "SKU", true, "Unique stock-keeping code of this variant.", 50);
        public static readonly UploadColumn Barcode = new(5, "Barcode", false, "Optional.", 50);
        public static readonly UploadColumn SellingPrice = new(6, "Selling Price", true, "0 or more, e.g. 25000 or 1250.50.");
        public static readonly UploadColumn IsSerialized = new(7, "Is Serialized", false, "Yes if every unit has its own serial / IMEI number. Empty = No.");
        public static readonly UploadColumn Attributes = new(8, "Attributes", false, "Optional, as 'Name: Value' pairs split by ';', e.g. Color: Black; Storage: 128GB.");
        public static readonly UploadColumn Description = new(9, "Product Description", false, "Optional. Used only when a new product is created.", 500);

        public static readonly IReadOnlyList<UploadColumn> Columns =
            [ProductName, SubCategoryCode, BrandName, Sku, Barcode, SellingPrice, IsSerialized, Attributes, Description];

        public DataUploadType Type => DataUploadType.Product;

        public async Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken)
        {
            var duplicateSkus = UploadRules.DuplicatesInFile(rows.Select(r => (r[Sku], r.RowNumber)));

            // SKU is unique in the database for every variant, deleted ones included.
            var skusInFile = rows.Select(r => r[Sku].ToLower()).Where(s => s.Length > 0).Distinct().ToList();
            var existingSkus = (await _db.ProductVariants.AsNoTracking()
                    .Where(v => skusInFile.Contains(v.SKU.ToLower()))
                    .Select(v => v.SKU)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Tracked on purpose: the new products point at these rows.
            var subCategories = UploadRules.ByKey(
                await _db.ProductSubCategories.Where(s => s.IsActive != (int)EntityStatus.Deleted).ToListAsync(cancellationToken),
                s => s.Code);
            var brands = UploadRules.ByKey(
                await _db.Brands.Where(b => b.IsActive != (int)EntityStatus.Deleted).ToListAsync(cancellationToken),
                b => b.Name);

            var errors = new List<RowError>();
            var valid = new List<(UploadRow Row, string Name, ProductSubCategories Sub, Brand Brand, ProductVariant Variant)>();

            foreach (var row in rows)
            {
                var check = new RowCheck(row);
                var name = check.Text(ProductName);
                var subCode = check.Text(SubCategoryCode);
                var brandName = check.Text(BrandName);
                var sku = check.Text(Sku);
                var barcode = check.Text(Barcode);
                var price = check.Amount(SellingPrice);
                var isSerialized = check.YesNo(IsSerialized);
                var attributesJson = ParseAttributes(check, check.Text(Attributes));
                check.Text(Description);

                if (duplicateSkus.TryGetValue(sku, out var dupRows))
                    check.Fail($"SKU '{sku}' is also used in row(s) {UploadRules.OtherRows(dupRows, row.RowNumber)}.");

                if (sku.Length > 0 && existingSkus.Contains(sku))
                    check.Fail($"SKU '{sku}' already exists.");

                ProductSubCategories? sub = null;
                if (subCode.Length > 0 && !subCategories.TryGetValue(subCode, out sub))
                    check.Fail($"Sub Category Code '{subCode}' was not found.");

                Brand? brand = null;
                if (brandName.Length > 0 && !brands.TryGetValue(brandName, out brand))
                    check.Fail($"Brand '{brandName}' was not found.");

                if (!check.IsValid || sub is null || brand is null)
                {
                    errors.Add(check.ToError());
                    continue;
                }

                var variant = new ProductVariant
                {
                    SKU = sku,
                    Barcode = barcode,
                    SellingPrice = price,
                    IsSerialized = isSerialized,
                    AttributesJson = attributesJson,
                    Product = null!, // set below, once the product of its group is known
                    CreatedAt = DateTime.UtcNow,
                };
                valid.Add((row, name, sub, brand, variant));
            }

            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

            // Products that already exist for the same sub-category + brand get the new variants.
            var subIds = valid.Select(v => v.Sub.Id).Distinct().ToList();
            var brandIds = valid.Select(v => v.Brand.Id).Distinct().ToList();
            var existingProducts = (await _db.Products
                    .Where(p => subIds.Contains(p.ProductSubCategoryId)
                             && brandIds.Contains(p.BrandId)
                             && p.IsActive != (int)EntityStatus.Deleted)
                    .ToListAsync(cancellationToken))
                .GroupBy(p => GroupKey(p.ProductName, p.ProductSubCategoryId, p.BrandId))
                .ToDictionary(g => g.Key, g => g.First());

            var products = new Dictionary<string, Product>();
            var newProducts = 0;

            foreach (var item in valid)
            {
                var key = GroupKey(item.Name, item.Sub.Id, item.Brand.Id);

                if (!products.TryGetValue(key, out var product))
                {
                    if (!existingProducts.TryGetValue(key, out product))
                    {
                        product = new Product
                        {
                            ProductName = item.Name,
                            ProductDescription = item.Row[Description],
                            ProductSubCategoryId = item.Sub.Id,
                            ProductSubCategories = item.Sub,
                            BrandId = item.Brand.Id,
                            Brand = item.Brand,
                            CreatedAt = DateTime.UtcNow,
                        };
                        _db.Products.Add(product);
                        newProducts++;
                    }
                    products[key] = product;
                }

                item.Variant.Product = product;
                product.ProductVariants.Add(item.Variant);
                _db.ProductVariants.Add(item.Variant);
            }

            var addedToExisting = products.Count - newProducts;
            var summary = $"{valid.Count} variant(s) in {newProducts} new product(s)"
                + (addedToExisting > 0 ? $" and {addedToExisting} existing product(s)" : string.Empty);

            return new UploadCheckResult(errors, valid.Count, summary);
        }

        private static string GroupKey(string name, int subCategoryId, int brandId) =>
            $"{name.Trim().ToLowerInvariant()}|{subCategoryId}|{brandId}";

        /// <summary>"Color: Black; Storage: 128GB" -> {"Color":"Black","Storage":"128GB"}.</summary>
        private static string ParseAttributes(RowCheck check, string text)
        {
            if (text.Length == 0) return "{}";

            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = text.Split([';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var separator = part.IndexOfAny([':', '=']);
                var name = separator > 0 ? part[..separator].Trim() : string.Empty;
                var value = separator > 0 ? part[(separator + 1)..].Trim() : string.Empty;

                if (name.Length == 0 || value.Length == 0)
                {
                    check.Fail($"Attributes part '{part}' must look like 'Name: Value', e.g. Color: Black; Storage: 128GB.");
                    return "{}";
                }

                if (!attributes.TryAdd(name, value))
                {
                    check.Fail($"Attribute '{name}' is given more than once.");
                    return "{}";
                }
            }

            return JsonSerializer.Serialize(attributes);
        }
    }
}
