using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Shared.Extensions.LockExtensions
{
    // Row locks (SELECT ... FOR UPDATE) that make two requests touching the same rows
    // wait for each other instead of both reading the same old value.
    //
    // Rules for callers:
    //  - Call inside a transaction. The lock is held until commit or rollback.
    //  - Lock BEFORE loading the rows with EF, so the load reads the latest committed values.
    //  - Lock in this order to avoid deadlocks: document -> stock -> customer/supplier.
    public static class RowLockExtensions
    {
        // Locks one row of the table behind entity T by its Id.
        // The table name comes from the EF model, never from user input.
        public static Task LockRowAsync<T>(this AppDbContext db, int id, CancellationToken cancellationToken)
            where T : BaseEntity
        {
            var table = db.Model.FindEntityType(typeof(T))!.GetTableName();
#pragma warning disable EF1002 // Safe: the table name comes from the EF model; the id is a parameter.
            return db.Database.ExecuteSqlRawAsync(
                $"SELECT 1 FROM \"{table}\" WHERE \"Id\" = {{0}} FOR UPDATE",
                [id],
                cancellationToken);
#pragma warning restore EF1002
        }

        // Locks the stock rows of these variants at these branches, always in Id order,
        // so two requests that need the same rows never lock them in opposite order.
        // Serial numbers of a variant at a branch are protected by the same lock.
        public static Task LockStocksAsync(
            this AppDbContext db,
            IEnumerable<int> branchIds,
            IEnumerable<int> variantIds,
            CancellationToken cancellationToken)
        {
            var branches = branchIds.Distinct().ToArray();
            var variants = variantIds.Distinct().ToArray();

            return db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"Stocks\" WHERE \"BranchId\" = ANY({branches}) AND \"ProductVariantId\" = ANY({variants}) ORDER BY \"Id\" FOR UPDATE",
                cancellationToken);
        }
    }
}
