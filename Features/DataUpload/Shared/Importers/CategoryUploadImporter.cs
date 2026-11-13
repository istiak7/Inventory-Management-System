using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    public class CategoryUploadImporter(AppDbContext _db) : IUploadImporter
    {
        public static readonly UploadColumn Name = new(1, "Category Name", true, "Unique category name, e.g. Mobile Phone. Sub-categories refer to it.", 100);

        public static readonly IReadOnlyList<UploadColumn> Columns = [Name];

        public DataUploadType Type => DataUploadType.Category;

        public async Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken)
        {
            var duplicates = UploadRules.DuplicatesInFile(rows.Select(r => (r[Name], r.RowNumber)));

            var existing = await _db.ProductCategories.AsNoTracking()
                .Select(c => new { c.CategoryName, c.Code, c.IsActive })
                .ToListAsync(cancellationToken);

            var existingNames = existing
                .Where(c => c.IsActive != (int)EntityStatus.Deleted)
                .Select(c => c.CategoryName.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Codes are generated (the file has no code column); a code is never reused, even by a deleted category.
            var takenCodes = existing.Select(c => c.Code.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var errors = new List<RowError>();
            var categories = new List<ProductCategories>();

            foreach (var row in rows)
            {
                var check = new RowCheck(row);
                var name = check.Text(Name);

                if (duplicates.TryGetValue(name, out var dupRows))
                    check.Fail($"Category Name '{name}' is also used in row(s) {UploadRules.OtherRows(dupRows, row.RowNumber)}.");

                if (name.Length > 0 && existingNames.Contains(name))
                    check.Fail($"Category '{name}' already exists.");

                if (!check.IsValid)
                {
                    errors.Add(check.ToError());
                    continue;
                }

                categories.Add(new ProductCategories
                {
                    CategoryName = name,
                    Code = CatalogCodes.Unique(CatalogCodes.CodeBase(name), CatalogCodes.CodeMaxLength, takenCodes),
                    CreatedAt = DateTime.UtcNow,
                });
            }

            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

            _db.ProductCategories.AddRange(categories);
            return new UploadCheckResult(errors, categories.Count, $"{categories.Count} categor{(categories.Count == 1 ? "y" : "ies")}");
        }
    }
}
