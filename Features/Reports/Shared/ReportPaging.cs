using Inventory_Management_System.Shared.Extensions.PaginationExtensions;

namespace Inventory_Management_System.Features.Reports.Shared
{
    public static class ReportPaging
    {
        public const int MaxPageSize = 500;

        public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;
            return (pageNumber, pageSize);
        }

        public static int SkipCount(int pageNumber, int pageSize) => (pageNumber - 1) * pageSize;

        public static int TakeForMerge(int pageNumber, int pageSize) => pageNumber * pageSize;

        public static PagedResult<T> MergePage<T, TKey>(
            IEnumerable<T> first,
            IEnumerable<T> second,
            Func<T, TKey> orderBy,
            bool descending,
            long totalCount,
            int pageNumber,
            int pageSize)
        {
            var combined = first.Concat(second);

            var ordered = descending
                ? combined.OrderByDescending(orderBy)
                : combined.OrderBy(orderBy);

            var items = ordered
                .Skip(SkipCount(pageNumber, pageSize))
                .Take(pageSize)
                .ToList();

            return Page(items, totalCount, pageNumber, pageSize);
        }

        public static PagedResult<T> Page<T>(IReadOnlyList<T> items, long totalCount, int pageNumber, int pageSize) =>
            new()
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
    }
}
