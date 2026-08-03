using Inventory_Management_System.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Shared.Extensions.LedgerExtensions
{
    public static class LedgerQueryExtensions
    {
        /// <summary>
        /// Latest running balance in an append-only ledger — the BalanceAfter of the most
        /// recently inserted row (highest Id), or 0 when the ledger has none yet. Callers scope
        /// the ledger to one party first, e.g. `_dbContext.CustomerTransactions.Where(t => t.CustomerId == id)`.
        /// </summary>
        public static async Task<decimal> GetLatestBalanceAsync<T>(
            this IQueryable<T> ledger, CancellationToken cancellationToken)
            where T : class, ILedgerEntry
        {
            return await ledger
                .AsNoTracking()
                .OrderByDescending(e => e.Id)
                .Select(e => e.BalanceAfter)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
