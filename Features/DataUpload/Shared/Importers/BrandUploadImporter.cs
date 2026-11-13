using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    public class BrandUploadImporter(AppDbContext _db) : IUploadImporter
    {
        public static readonly UploadColumn Name = new(1, "Brand Name", true, "Unique brand name, e.g. Samsung.", 200);

        public static readonly IReadOnlyList<UploadColumn> Columns = [Name];

        public DataUploadType Type => DataUploadType.Brand;

        public async Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken)
        {
            var duplicates = UploadRules.DuplicatesInFile(rows.Select(r => (r[Name], r.RowNumber)));

            var existingNames = (await _db.Brands.AsNoTracking()
                    .Where(b => b.IsActive != (int)EntityStatus.Deleted)
                    .Select(b => b.Name)
                    .ToListAsync(cancellationToken))
                .Select(n => n.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var errors = new List<RowError>();
            var brands = new List<Brand>();

            foreach (var row in rows)
            {
                var check = new RowCheck(row);
                var name = check.Text(Name);

                if (duplicates.TryGetValue(name, out var dupRows))
                    check.Fail($"Brand Name '{name}' is also used in row(s) {UploadRules.OtherRows(dupRows, row.RowNumber)}.");

                if (name.Length > 0 && existingNames.Contains(name))
                    check.Fail($"Brand '{name}' already exists.");

                if (!check.IsValid)
                {
                    errors.Add(check.ToError());
                    continue;
                }

                brands.Add(new Brand { Name = name, CreatedAt = DateTime.UtcNow });
            }

            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

            _db.Brands.AddRange(brands);
            return new UploadCheckResult(errors, brands.Count, $"{brands.Count} brand(s)");
        }
    }
}
