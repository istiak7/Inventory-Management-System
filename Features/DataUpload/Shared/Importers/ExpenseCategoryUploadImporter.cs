using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    public class ExpenseCategoryUploadImporter(AppDbContext _db) : IUploadImporter
    {
        public static readonly UploadColumn Name = new(1, "Category Name", true, "e.g. Electricity Bill, Office Rent.", 100);
        public static readonly UploadColumn Description = new(2, "Description", false, "Optional.", 500);

        public static readonly IReadOnlyList<UploadColumn> Columns = [Name, Description];

        public DataUploadType Type => DataUploadType.ExpenseCategory;

        public async Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken)
        {
            var duplicates = UploadRules.DuplicatesInFile(rows.Select(r => (r[Name], r.RowNumber)));

            var existingNames = (await _db.ExpenseCategories.AsNoTracking()
                    .Where(c => c.IsActive != (int)EntityStatus.Deleted)
                    .Select(c => c.Name)
                    .ToListAsync(cancellationToken))
                .Select(n => n.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var errors = new List<RowError>();
            var categories = new List<ExpenseCategory>();

            foreach (var row in rows)
            {
                var check = new RowCheck(row);
                var name = check.Text(Name);
                var description = check.Text(Description);

                if (duplicates.TryGetValue(name, out var dupRows))
                    check.Fail($"Category Name '{name}' is also used in row(s) {UploadRules.OtherRows(dupRows, row.RowNumber)}.");

                if (name.Length > 0 && existingNames.Contains(name))
                    check.Fail($"Expense category '{name}' already exists.");

                if (!check.IsValid)
                {
                    errors.Add(check.ToError());
                    continue;
                }

                categories.Add(ExpenseCategory.Create(name, description.Length == 0 ? null : description));
            }

            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

            _db.ExpenseCategories.AddRange(categories);
            return new UploadCheckResult(errors, categories.Count, $"{categories.Count} expense categor{(categories.Count == 1 ? "y" : "ies")}");
        }
    }
}
