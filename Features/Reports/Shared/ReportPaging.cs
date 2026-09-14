using Inventory_Management_System.Shared.Extensions.PaginationExtensions;

namespace Inventory_Management_System.Features.Reports.Shared
{
    public static class ReportPaging
    {
        public const int MaxPageSize = 500;

        /// <summary>
        /// An export writes the whole filtered result set as a single page, so it is allowed past
        /// <see cref="MaxPageSize"/>. The ceiling still exists: it stops one request from pulling an
        /// unbounded table into memory while the workbook is built.
        /// </summary>
        public const int MaxExportPageSize = 50_000;

        /// <param name="forExport">
        /// Only the export endpoints pass true. It is not bindable from the query string, so a normal
        /// report request cannot page past <see cref="MaxPageSize"/> by asking nicely.
        /// </param>
        public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize, bool forExport = false)
        {
            var max = forExport ? MaxExportPageSize : MaxPageSize;
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > max) pageSize = max;
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
