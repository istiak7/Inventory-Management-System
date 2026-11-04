using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    public class SupplierUploadImporter(AppDbContext _db) : IUploadImporter
    {
        public static readonly UploadColumn Name = new(1, "Supplier Name", true, "Company or person name.", 100);
        public static readonly UploadColumn Group = new(2, "Group", true, "Supplier group, e.g. Local, Importer.", 100);
        public static readonly UploadColumn Phone = new(3, "Phone Number", true, "Format the column as Text so the leading 0 is kept.", 20);
        public static readonly UploadColumn Email = new(4, "Email", true, "Must be unique for every supplier.", 100);
        public static readonly UploadColumn Nid = new(5, "NID", false, "Optional national ID number.", 20);
        public static readonly UploadColumn OpeningBalance = new(6, "Opening Balance", false, "Amount you owe this supplier today. Empty = 0.");
        public static readonly UploadColumn Description = new(7, "Description", false, "Optional.", 500);

        public static readonly IReadOnlyList<UploadColumn> Columns = [Name, Group, Phone, Email, Nid, OpeningBalance, Description];

        public DataUploadType Type => DataUploadType.Supplier;

        public async Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken)
        {
            var duplicateEmails = UploadRules.DuplicatesInFile(rows.Select(r => (r[Email], r.RowNumber)));
            var duplicateNamePhones = UploadRules.DuplicatesInFile(rows.Select(r => (NamePhoneKey(r[Name], r[Phone]), r.RowNumber)));

            // Email has a unique index over every supplier, deleted ones included.
            var emailsInFile = rows.Select(r => r[Email].ToLower()).Where(e => e.Length > 0).Distinct().ToList();
            var existingEmails = (await _db.Suppliers.AsNoTracking()
                    .Where(s => emailsInFile.Contains(s.Email.ToLower()))
                    .Select(s => s.Email)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Same rule as the Create Supplier screen: name + phone may not repeat.
            var phonesInFile = rows.Select(r => r[Phone]).Where(p => p.Length > 0).Distinct().ToList();
            var existingNamePhones = (await _db.Suppliers.AsNoTracking()
                    .Where(s => phonesInFile.Contains(s.PhoneNumber) && s.IsActive != (int)EntityStatus.Deleted)
                    .Select(s => new { s.Name, s.PhoneNumber })
                    .ToListAsync(cancellationToken))
                .Select(s => NamePhoneKey(s.Name, s.PhoneNumber))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var errors = new List<RowError>();
            var suppliers = new List<Supplier>();

            foreach (var row in rows)
            {
                var check = new RowCheck(row);
                var name = check.Text(Name);
                var group = check.Text(Group);
                var phone = check.Text(Phone);
                var email = check.Email(Email);
                var nid = check.Text(Nid);
                var openingBalance = check.Amount(OpeningBalance);
                var description = check.Text(Description);

                if (duplicateEmails.TryGetValue(email, out var emailRows))
                    check.Fail($"Email '{email}' is also used in row(s) {UploadRules.OtherRows(emailRows, row.RowNumber)}.");
                else if (email.Length > 0 && existingEmails.Contains(email))
                    check.Fail($"A supplier with email '{email}' already exists.");

                var namePhone = NamePhoneKey(name, phone);
                if (duplicateNamePhones.TryGetValue(namePhone, out var namePhoneRows))
                    check.Fail($"Supplier '{name}' with phone {phone} is also in row(s) {UploadRules.OtherRows(namePhoneRows, row.RowNumber)}.");
                else if (name.Length > 0 && phone.Length > 0 && existingNamePhones.Contains(namePhone))
                    check.Fail($"Supplier '{name}' with phone {phone} already exists.");

                if (!check.IsValid)
                {
                    errors.Add(check.ToError());
                    continue;
                }

                var supplier = new Supplier
                {
                    Name = name,
                    Group = group,
                    PhoneNumber = phone,
                    Email = email,
                    NID = nid,
                    OpeningBalance = openingBalance,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                };

                // Same as the Create Supplier screen: the opening balance goes into the ledger.
                if (supplier.OpeningBalance != 0)
                    supplier.SupplierTransactions.Add(SupplierTransaction.ForOpening(supplier));

                suppliers.Add(supplier);
            }

            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

            _db.Suppliers.AddRange(suppliers);
            return new UploadCheckResult(errors, suppliers.Count, $"{suppliers.Count} supplier(s)");
        }

        private static string NamePhoneKey(string name, string phone) =>
            name.Length == 0 || phone.Length == 0 ? string.Empty : $"{name.Trim()}|{phone.Trim()}";
    }
}
