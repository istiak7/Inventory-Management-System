using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    public class CategoryUploadImporter(AppDbContext _db) : IUploadImporter
    {
        public static readonly UploadColumn Name = new(1, "Category Name", true, "e.g. Mobile Phone.", 100);
        public static readonly UploadColumn Code = new(2, "Category Code", true, "Unique short code, e.g. MOB. Sub-categories refer to it.", 50);
        public static readonly UploadColumn Description = new(3, "Description", false, "Optional.", 500);

        public static readonly IReadOnlyList<UploadColumn> Columns = [Name, Code, Description];

        public DataUploadType Type => DataUploadType.Category;

        public async Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken)
        {
            var duplicates = UploadRules.DuplicatesInFile(rows.Select(r => (r[Code], r.RowNumber)));

            // Same rule as the Create Category screen: a code may not be used again, even by a deleted category.
            var existingCodes = (await _db.ProductCategories.AsNoTracking()
                    .Select(c => c.Code)
                    .ToListAsync(cancellationToken))
                .Select(c => c.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var errors = new List<RowError>();
            var categories = new List<ProductCategories>();

            foreach (var row in rows)
            {
                var check = new RowCheck(row);
                var name = check.Text(Name);
                var code = check.Text(Code);
                var description = check.Text(Description);

                if (duplicates.TryGetValue(code, out var dupRows))
                    check.Fail($"Category Code '{code}' is also used in row(s) {UploadRules.OtherRows(dupRows, row.RowNumber)}.");

                if (code.Length > 0 && existingCodes.Contains(code))
                    check.Fail($"Category Code '{code}' already exists.");

                if (!check.IsValid)
                {
                    errors.Add(check.ToError());
                    continue;
                }

                categories.Add(new ProductCategories
                {
                    CategoryName = name,
                    Code = code,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

            _db.ProductCategories.AddRange(categories);
            return new UploadCheckResult(errors, categories.Count, $"{categories.Count} categor{(categories.Count == 1 ? "y" : "ies")}");
        }
    }
}
