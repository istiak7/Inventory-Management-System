using System.Globalization;
using System.Net.Mail;

namespace Inventory_Management_System.Features.DataUpload.Shared.Importers
{
    /// <summary>Outcome of checking a file: the row errors, or what will be created.</summary>
    /// <param name="Errors">Every failed row. Empty = the file is good.</param>
    /// <param name="CreatedRecords">Number of new records added to the DbContext (0 when there are errors).</param>
    /// <param name="Summary">Human text, e.g. "3 products with 7 variants".</param>
    public sealed record UploadCheckResult(IReadOnlyList<RowError> Errors, int CreatedRecords, string Summary);

    /// <summary>
    /// Checks the rows of one upload type against each other and against the database.
    /// When — and only when — every row is valid, the new records are added to the DbContext
    /// (not saved). The caller saves them in one SaveChanges, so the file is saved completely or not at all.
    /// </summary>
    public interface IUploadImporter
    {
        DataUploadType Type { get; }

        Task<UploadCheckResult> CheckAndStageAsync(IReadOnlyList<UploadRow> rows, CancellationToken cancellationToken);
    }

    /// <summary>Collects the problems of one row while its cells are read.</summary>
    public sealed class RowCheck(UploadRow row)
    {
        private readonly List<string> _messages = [];

        public UploadRow Row { get; } = row;
        public bool IsValid => _messages.Count == 0;

        public void Fail(string message) => _messages.Add(message);

        public RowError ToError() => new(Row.RowNumber, _messages.ToList());

        /// <summary>Trimmed text; checks "required" and the maximum length.</summary>
        public string Text(UploadColumn column)
        {
            var value = Row[column];

            if (value.Length == 0)
            {
                if (column.Required) Fail($"{column.Name} is required.");
                return string.Empty;
            }

            if (column.MaxLength is int max && value.Length > max)
                Fail($"{column.Name} must be {max} characters or fewer (it has {value.Length}).");

            return value;
        }

        /// <summary>A money amount, 0 or more. Empty = <paramref name="whenEmpty"/>. Accepts "1,250.50".</summary>
        public decimal Amount(UploadColumn column, decimal whenEmpty = 0m)
        {
            var value = Text(column);
            if (value.Length == 0) return whenEmpty;

            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                Fail($"{column.Name} '{value}' is not a valid number.");
                return 0m;
            }

            if (amount < 0)
                Fail($"{column.Name} cannot be negative.");

            if (decimal.Round(amount, 2) != amount)
                Fail($"{column.Name} can have at most 2 decimal places.");

            return amount;
        }

        /// <summary>Yes/No. Empty = No.</summary>
        public bool YesNo(UploadColumn column)
        {
            var value = Text(column).ToLowerInvariant();
            switch (value)
            {
                case "": case "no": case "n": case "false": case "0":
                    return false;
                case "yes": case "y": case "true": case "1":
                    return true;
                default:
                    Fail($"{column.Name} must be Yes or No (found '{Row[column]}').");
                    return false;
            }
        }

        /// <summary>Email address, checked for a valid shape. Lower-cased.</summary>
        public string Email(UploadColumn column)
        {
            var value = Text(column);
            if (value.Length == 0) return string.Empty;

            if (!MailAddress.TryCreate(value, out var address) || address.Address != value)
                Fail($"{column.Name} '{value}' is not a valid email address.");

            return value.ToLowerInvariant();
        }
    }

    public static class UploadRules
    {
        /// <summary>
        /// Finds values used by more than one row of the file (case-insensitive) and returns,
        /// for each duplicated key, the rows that use it.
        /// </summary>
        public static Dictionary<string, List<int>> DuplicatesInFile(IEnumerable<(string Key, int RowNumber)> keys) =>
            keys.Where(k => k.Key.Length > 0)
                .GroupBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.Select(k => k.RowNumber).ToList(), StringComparer.OrdinalIgnoreCase);

        public static string OtherRows(List<int> rows, int current) =>
            string.Join(", ", rows.Where(r => r != current));

        /// <summary>Lower-case lookup that keeps the first row when the database has the same key twice.</summary>
        public static Dictionary<string, T> ByKey<T>(IEnumerable<T> items, Func<T, string> key) =>
            items.GroupBy(i => key(i).Trim(), StringComparer.OrdinalIgnoreCase)
                 .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }
}
