using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    public class SubCategoryUploadImporter(AppDbContext _db) : IUploadImporter
    {
        public static readonly UploadColumn Name = new(1, "Sub Category Name", true, "e.g. Smartphone.", 100);
        public static readonly UploadColumn Code = new(2, "Sub Category Code", true, "Unique short code, e.g. MOB-SMART. Products refer to it.", 50);
        public static readonly UploadColumn CategoryCode = new(3, "Category Code", true, "Code of an existing category, e.g. MOB.", 50);
        public static readonly UploadColumn Description = new(4, "Description", false, "Optional.", 500);

        public static readonly IReadOnlyList<UploadColumn> Columns = [Name, Code, CategoryCode, Description];

        public DataUploadType Type => DataUploadType.SubCategory;

        public async Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken)
        {
            var duplicates = UploadRules.DuplicatesInFile(rows.Select(r => (r[Code], r.RowNumber)));

            var existingCodes = (await _db.ProductSubCategories.AsNoTracking()
                    .Select(s => s.Code)
                    .ToListAsync(cancellationToken))
                .Select(c => c.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Tracked on purpose: the new sub-categories point at these rows.
            var categories = UploadRules.ByKey(
                await _db.ProductCategories
                    .Where(c => c.IsActive != (int)EntityStatus.Deleted)
                    .ToListAsync(cancellationToken),
                c => c.Code);

            var errors = new List<RowError>();
            var subCategories = new List<ProductSubCategories>();

            foreach (var row in rows)
            {
                var check = new RowCheck(row);
                var name = check.Text(Name);
                var code = check.Text(Code);
                var categoryCode = check.Text(CategoryCode);
                var description = check.Text(Description);

                if (duplicates.TryGetValue(code, out var dupRows))
                    check.Fail($"Sub Category Code '{code}' is also used in row(s) {UploadRules.OtherRows(dupRows, row.RowNumber)}.");

                if (code.Length > 0 && existingCodes.Contains(code))
                    check.Fail($"Sub Category Code '{code}' already exists.");

                ProductCategories? category = null;
                if (categoryCode.Length > 0 && !categories.TryGetValue(categoryCode, out category))
                    check.Fail($"Category Code '{categoryCode}' was not found. Upload or create the category first.");

                if (!check.IsValid || category is null)
                {
                    errors.Add(check.ToError());
                    continue;
                }

                subCategories.Add(new ProductSubCategories
                {
                    SubCategoryName = name,
                    Code = code,
                    Description = description,
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
