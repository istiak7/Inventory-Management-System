namespace Inventory_Management_System.Features.DataUpload.Shared
{
    /// <summary>A column found in the first row of the user's file.</summary>
    /// <param name="Column">Excel column letter, e.g. "C".</param>
    /// <param name="Name">Heading text in that column (may be empty).</param>
    /// <param name="Order">Column number, 1 = A.</param>
    public sealed record UploadedHeader(string Column, string Name, int Order);

    public static class MatchStatus
    {
        public const string Match = "Match";
        public const string PartialMatch = "PartialMatch";
        public const string NotMatch = "NotMatch";
    }

    /// <summary>How one system column was paired with a column of the user's file.</summary>
    public sealed record HeaderMatch(
        int SystemColumn,
        string SystemName,
        bool Required,
        string? UploadedColumn,
        string? UploadedName,
        string Status);

    /// <summary>
    /// Pairs the system columns with the columns of the uploaded file by their heading text.
    /// First exact matches ("Phone Number" = "phone number *"), then partial ones
    /// ("Phone" inside "Phone Number"), the closest partial pair first. Each file column is used once.
    /// </summary>
    public static class HeaderMatcher
    {
        // "SKU" is 3 letters, so shorter words than this are too weak to count as a partial match.
        private const int MinPartialLength = 3;

        // Only letters and digits count: spaces, '*', '-', '_' and case are ignored.
        public static string Normalize(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        public static List<HeaderMatch> Match(IReadOnlyList<UploadColumn> systemColumns, IReadOnlyList<UploadedHeader> uploaded)
        {
            var paired = new Dictionary<int, (UploadedHeader Header, string Status)>();
            var taken = new HashSet<int>();

            // 1. Exact matches.
            foreach (var column in systemColumns)
            {
                var name = Normalize(column.Name);
                var exact = uploaded.FirstOrDefault(u => !taken.Contains(u.Order) && Normalize(u.Name) == name);
                if (exact is null) continue;

                paired[column.Order] = (exact, MatchStatus.Match);
                taken.Add(exact.Order);
            }

            // 2. Partial matches, best overlap first.
            var candidates = systemColumns
                .Where(c => !paired.ContainsKey(c.Order))
                .SelectMany(c => uploaded
                    .Where(u => !taken.Contains(u.Order) && IsPartial(Normalize(c.Name), Normalize(u.Name)))
                    .Select(u => new { Column = c, Header = u, Overlap = Overlap(Normalize(c.Name), Normalize(u.Name)) }))
                .OrderByDescending(x => x.Overlap)
                .ThenBy(x => x.Column.Order)
                .ThenBy(x => x.Header.Order);

            foreach (var candidate in candidates)
            {
                if (paired.ContainsKey(candidate.Column.Order) || taken.Contains(candidate.Header.Order)) continue;

                paired[candidate.Column.Order] = (candidate.Header, MatchStatus.PartialMatch);
                taken.Add(candidate.Header.Order);
            }

            return systemColumns.Select(c => paired.TryGetValue(c.Order, out var p)
                    ? new HeaderMatch(c.Order, c.Name, c.Required, p.Header.Column, p.Header.Name, p.Status)
                    : new HeaderMatch(c.Order, c.Name, c.Required, null, null, MatchStatus.NotMatch))
                .ToList();
        }

        private static bool IsPartial(string system, string uploaded) =>
            system.Length >= MinPartialLength
            && uploaded.Length >= MinPartialLength
            && (system.Contains(uploaded) || uploaded.Contains(system));

        private static double Overlap(string a, string b) =>
            (double)Math.Min(a.Length, b.Length) / Math.Max(a.Length, b.Length);
    }
}
