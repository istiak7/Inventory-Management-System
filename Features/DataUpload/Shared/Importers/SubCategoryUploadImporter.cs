using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    public class SubCategoryUploadImporter(AppDbContext _db) : IUploadImporter
    {
        public static readonly UploadColumn Name = new(1, "Sub Category Name", true, "e.g. Smartphone. Products refer to it.", 100);
        public static readonly UploadColumn CategoryName = new(2, "Category Name", true, "Name of an existing category, e.g. Mobile Phone.", 100);

        public static readonly IReadOnlyList<UploadColumn> Columns = [Name, CategoryName];

        public DataUploadType Type => DataUploadType.SubCategory;

        public async Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken)
        {
            // Same sub-category name may exist under different categories, so the pair is the key.
            var duplicates = UploadRules.DuplicatesInFile(rows.Select(r => ($"{r[Name]}|{r[CategoryName]}", r.RowNumber)));

            var existing = await _db.ProductSubCategories.AsNoTracking()
                .Select(s => new { s.SubCategoryName, s.Code, s.ProductCategoryId, s.IsActive })
                .ToListAsync(cancellationToken);

            var takenCodes = existing.Select(s => s.Code.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Tracked on purpose: the new sub-categories point at these rows.
            var categoriesByName = (await _db.ProductCategories
                    .Where(c => c.IsActive != (int)EntityStatus.Deleted)
                    .ToListAsync(cancellationToken))
                .GroupBy(c => c.CategoryName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var existingPairs = existing
                .Where(s => s.IsActive != (int)EntityStatus.Deleted)
                .Select(s => $"{s.SubCategoryName.Trim()}|{s.ProductCategoryId}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var errors = new List<RowError>();
            var subCategories = new List<ProductSubCategories>();

            foreach (var row in rows)
            {
                var check = new RowCheck(row);
                var name = check.Text(Name);
                var categoryName = check.Text(CategoryName);

                if (duplicates.TryGetValue($"{name}|{categoryName}", out var dupRows))
                    check.Fail($"Sub Category '{name}' under '{categoryName}' is also used in row(s) {UploadRules.OtherRows(dupRows, row.RowNumber)}.");

                ProductCategories? category = null;
                if (categoryName.Length > 0)
                {
                    if (!categoriesByName.TryGetValue(categoryName, out var matches))
                        check.Fail($"Category '{categoryName}' was not found. Upload or create the category first.");
                    else if (matches.Count > 1)
                        check.Fail($"More than one category is named '{categoryName}'. Rename one of them first.");
                    else
                        category = matches[0];
                }

                if (category is not null && name.Length > 0 && existingPairs.Contains($"{name}|{category.Id}"))
                    check.Fail($"Sub Category '{name}' already exists under '{categoryName}'.");

                if (!check.IsValid || category is null)
                {
                    errors.Add(check.ToError());
                    continue;
                }

                subCategories.Add(new ProductSubCategories
                {
                    SubCategoryName = name,
                    Code = CatalogCodes.Unique(CatalogCodes.CodeBase(name), CatalogCodes.CodeMaxLength, takenCodes),
                    ProductCategoryId = category.Id,
                    ProductCategories = category,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

            _db.ProductSubCategories.AddRange(subCategories);
            return new UploadCheckResult(errors, subCategories.Count, $"{subCategories.Count} sub-categor{(subCategories.Count == 1 ? "y" : "ies")}");
        }
    }
}
