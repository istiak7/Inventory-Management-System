using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Customers.Shared;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    public class CustomerUploadImporter(AppDbContext _db) : IUploadImporter
    {
        public static readonly UploadColumn Name = new(1, "Customer Name", true, "Full name.", 100);
        public static readonly UploadColumn Phone = new(2, "Phone Number", true, $"At least {CustomerPhoneNumber.MinimumDigits} digits, unique per customer. Format the column as Text so the leading 0 is kept.", 20);
        public static readonly UploadColumn Group = new(3, "Group", false, "Optional customer group, e.g. Retail, Corporate.", 100);
        public static readonly UploadColumn Email = new(4, "Email", false, "Optional.", 100);
        public static readonly UploadColumn Address = new(5, "Address", false, "Optional.", 250);
        public static readonly UploadColumn Nid = new(6, "NID", false, "Optional national ID number.", 20);
        public static readonly UploadColumn OpeningBalance = new(7, "Opening Balance", false, "Amount this customer owes you today. Empty = 0.");
        public static readonly UploadColumn Description = new(8, "Description", false, "Optional.", 500);

        public static readonly IReadOnlyList<UploadColumn> Columns = [Name, Phone, Group, Email, Address, Nid, OpeningBalance, Description];

        public DataUploadType Type => DataUploadType.Customer;

        public async Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken)
        {
            // Phones are compared the way they are stored: digits only, "+880 17…" = "017…".
            var duplicatePhones = UploadRules.DuplicatesInFile(rows.Select(r => (CustomerPhoneNumber.Normalize(r[Phone]), r.RowNumber)));

            // Phone number has a unique index over every customer, deleted ones included.
            var phonesInFile = rows.Select(r => CustomerPhoneNumber.Normalize(r[Phone])).Where(p => p.Length > 0).Distinct().ToList();
            var existingPhones = (await _db.Customers.AsNoTracking()
                    .Where(c => phonesInFile.Contains(c.PhoneNumber))
                    .Select(c => c.PhoneNumber)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            var errors = new List<RowError>();
            var customers = new List<Customer>();

            foreach (var row in rows)
            {
                var check = new RowCheck(row);
                var name = check.Text(Name);
                var rawPhone = check.Text(Phone);
                var group = check.Text(Group);
                var email = check.Email(Email);
                var address = check.Text(Address);
                var nid = check.Text(Nid);
                var openingBalance = check.Amount(OpeningBalance);
                var description = check.Text(Description);

                var phone = CustomerPhoneNumber.Normalize(rawPhone);
                if (rawPhone.Length > 0 && !CustomerPhoneNumber.IsValid(rawPhone))
                    check.Fail($"Phone Number '{rawPhone}' must contain at least {CustomerPhoneNumber.MinimumDigits} digits.");
                else if (duplicatePhones.TryGetValue(phone, out var phoneRows))
                    check.Fail($"Phone Number {phone} is also used in row(s) {UploadRules.OtherRows(phoneRows, row.RowNumber)}.");
                else if (phone.Length > 0 && existingPhones.Contains(phone))
                    check.Fail($"A customer with phone number {phone} already exists.");

                if (!check.IsValid)
                {
                    errors.Add(check.ToError());
                    continue;
                }

                var customer = new Customer
                {
                    Name = name,
                    PhoneNumber = phone,
                    Group = group,
                    Email = email,
                    Address = address,
                    NID = nid,
                    OpeningBalance = openingBalance,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                };

                // Same as the Create Customer screen: the opening balance goes into the ledger.
                if (customer.OpeningBalance != 0)
                    customer.CustomerTransactions.Add(CustomerTransaction.ForOpening(customer));

                customers.Add(customer);
            }

            if (errors.Count > 0) return new UploadCheckResult(errors, 0, string.Empty);

            _db.Customers.AddRange(customers);
            return new UploadCheckResult(errors, customers.Count, $"{customers.Count} customer(s)");
        }
    }
}
