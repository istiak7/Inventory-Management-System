using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    /// <summary>
    /// One row = one variant. Rows with the same Product Name + Sub Category + Brand are variants of
    /// one product. If that product already exists, the new variants are added to it. The SKU is
    /// generated (sub-category code + product name), as on the Product screen.
    /// </summary>
    public class ProductUploadImporter(AppDbContext _db) : IUploadImporter
    {
        public static readonly UploadColumn ProductName = new(1, "Product Name", true, "Rows with the same name, sub-category and brand become one product.", 100);
        public static readonly UploadColumn SubCategoryName = new(2, "Sub Category Name", true, "Name of an existing sub-category.", 100);
        public static readonly UploadColumn BrandName = new(3, "Brand Name", true, "Name of an existing brand.", 200);
        public static readonly UploadColumn SellingPrice = new(4, "Selling Price", true, "0 or more, e.g. 25000 or 1250.50.");
        public static readonly UploadColumn IsSerialized = new(5, "Is Serialized", false, "Yes if every unit has its own serial / IMEI number. Empty = No.");

        public static readonly IReadOnlyList<UploadColumn> Columns =
            [ProductName, SubCategoryName, BrandName, SellingPrice, IsSerialized];

        public DataUploadType Type => DataUploadType.Product;

        public async Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken)
        {
            // Every SKU in the database counts, deleted variants included (unique index).
            var takenSkus = (await _db.ProductVariants.AsNoTracking().Select(v => v.SKU).ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Tracked on purpose: the new products point at these rows.
            var subCategories = (await _db.ProductSubCategories.Where(s => s.IsActive != (int)EntityStatus.Deleted).ToListAsync(cancellationToken))
                .GroupBy(s => s.SubCategoryName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var brands = UploadRules.ByKey(
                await _db.Brands.Where(b => b.IsActive != (int)EntityStatus.Deleted).ToListAsync(cancellationToken),
                b => b.Name);

            var errors = new List<RowError>();
            var valid = new List<(UploadRow Row, string Name, ProductSubCategories Sub, Brand Brand, decimal Price, bool IsSerialized)>();

            foreach (var row in rows)
            {
                var check = new RowCheck(row);
                var name = check.Text(ProductName);
                var subName = check.Text(SubCategoryName);
                var brandName = check.Text(BrandName);
                var price = check.Amount(SellingPrice);
                var isSerialized = check.YesNo(IsSerialized);

                ProductSubCategories? sub = null;
                if (subName.Length > 0)
                {
                    if (!subCategories.TryGetValue(subName, out var matches))
                        check.Fail($"Sub Category '{subName}' was not found.");
                    else if (matches.Count > 1)
                        check.Fail($"More than one sub-category is named '{subName}'. Rename one of them first.");
                    else
                        sub = matches[0];
                }

                Brand? brand = null;
                if (brandName.Length > 0 && !brands.TryGetValue(brandName, out brand))
                    check.Fail($"Brand '{brandName}' was not found.");

                if (!check.IsValid || sub is null || brand is null)
                {
                    errors.Add(check.ToError());
                    continue;
                }

                valid.Add((row, name, sub, brand, price, isSerialized));
            }

            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

            // Without a SKU column, the same product + price + type twice is almost surely a repeated
            // row (or a re-uploaded file), so it is refused instead of silently creating a copy.
            var duplicateRows = UploadRules.DuplicatesInFile(valid.Select(v => (VariantKey(v.Name, v.Sub.Id, v.Brand.Id, v.Price, v.IsSerialized), v.Row.RowNumber)));
            foreach (var v in valid)
            {
                if (duplicateRows.TryGetValue(VariantKey(v.Name, v.Sub.Id, v.Brand.Id, v.Price, v.IsSerialized), out var dupRows))
                    errors.Add(new RowError(v.Row.RowNumber,
                        [$"Same product, price and type as row(s) {UploadRules.OtherRows(dupRows, v.Row.RowNumber)}."]));
            }
            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

            // Products that already exist for the same sub-category + brand get the new variants.
            var subIds = valid.Select(v => v.Sub.Id).Distinct().ToList();
            var brandIds = valid.Select(v => v.Brand.Id).Distinct().ToList();
            var existingProductList = await _db.Products
                .Include(p => p.ProductVariants)
                .Where(p => subIds.Contains(p.ProductSubCategoryId)
                         && brandIds.Contains(p.BrandId)
                         && p.IsActive != (int)EntityStatus.Deleted)
                .ToListAsync(cancellationToken);
            var existingProducts = existingProductList
                .GroupBy(p => GroupKey(p.ProductName, p.ProductSubCategoryId, p.BrandId))
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var v in valid)
            {
                if (existingProducts.TryGetValue(GroupKey(v.Name, v.Sub.Id, v.Brand.Id), out var found)
                    && found.ProductVariants.Any(x => x.SellingPrice == v.Price && x.IsSerialized == v.IsSerialized
                                                      && x.IsActive != (int)EntityStatus.Deleted))
                    errors.Add(new RowError(v.Row.RowNumber, [$"'{v.Name}' already has a variant with this price and type."]));
            }
            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

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

                var variant = new ProductVariant
                {
                    SKU = CatalogCodes.Unique(CatalogCodes.SkuBase(item.Sub.Code, item.Name), CatalogCodes.SkuMaxLength, takenSkus),
                    SellingPrice = item.Price,
                    IsSerialized = item.IsSerialized,
                    Product = product,
                    CreatedAt = DateTime.UtcNow,
                };
                product.ProductVariants.Add(variant);
                _db.ProductVariants.Add(variant);
            }

            var addedToExisting = products.Count - newProducts;
            var summary = $"{valid.Count} variant(s) in {newProducts} new product(s)"
                + (addedToExisting > 0 ? $" and {addedToExisting} existing product(s)" : string.Empty);

            return new UploadCheckResult(errors, valid.Count, summary);
        }

        private static string GroupKey(string name, int subCategoryId, int brandId) =>
            $"{name.Trim().ToLowerInvariant()}|{subCategoryId}|{brandId}";

        private static string VariantKey(string name, int subCategoryId, int brandId, decimal price, bool isSerialized) =>
            $"{GroupKey(name, subCategoryId, brandId)}|{price}|{isSerialized}";
    }
}
