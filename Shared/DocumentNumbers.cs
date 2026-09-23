using Inventory_Management_System.Database;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Inventory_Management_System.Shared
{
    /// <summary>
    /// Next automatic document number, e.g. "INV-2026-0042".
    ///  - Looks at ALL branches (the number is unique across the company, but staff only "see"
    ///    their own branch through the query filters).
    ///  - Only looks at numbers in the automatic format, so a hand-typed number like
    ///    "INV-2026-A7" cannot break the sequence.
    ///  - Two requests take turns (a database lock per prefix, held until the transaction ends),
    ///    so they cannot both get the same number. Call it inside a transaction.
    /// </summary>
    public static class DocumentNumbers
    {
        public static async Task<string> NextAsync(
            AppDbContext db,
            IQueryable<string> existingNumbers,
            string prefix,
            CancellationToken cancellationToken)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({prefix}))", cancellationToken);

            var pattern = "^" + Regex.Escape(prefix) + "[0-9]+$";

            // Longest first, then highest: "…-10000" comes after "…-9999".
            var latest = await existingNumbers
                .Where(n => Regex.IsMatch(n, pattern))
                .OrderByDescending(n => n.Length)
                .ThenByDescending(n => n)
                .FirstOrDefaultAsync(cancellationToken);

            var next = 1;
            if (latest != null && int.TryParse(latest[prefix.Length..], out var lastSequence))
                next = lastSequence + 1;

            return prefix + next.ToString("D4");
        }
    }
}
