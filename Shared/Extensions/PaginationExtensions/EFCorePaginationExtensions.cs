using Microsoft.EntityFrameworkCore;
namespace Inventory_Management_System.Shared.Extensions.PaginationExtensions
{
    public static class EFCorePaginationExtensions
    {
        public const int MaxPageSize = 500;

        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> source,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            // A very large page would load a whole table at once.
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;


            var totalCount = await source.LongCountAsync(cancellationToken);
            var offset = (pageNumber - 1) * pageSize;

            var items = await source
                .Skip(offset)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
