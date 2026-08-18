using System.Linq.Expressions;

namespace Inventory_Management_System.Shared.Extensions.QueryableFilterExtensions
{
    public static class QueryableFilterExtensions
    {
        public static IQueryable<T> WhereDateRange<T>(this IQueryable<T> source,
                                                      Expression<Func<T, DateTime>> dateSelector,
                                                      DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue)
            {
                var param = dateSelector.Parameters[0];
                var body = Expression.GreaterThanOrEqual(dateSelector.Body, Expression.Constant(startDate.Value.Date));
                source = source.Where(Expression.Lambda<Func<T, bool>>(body, param));
            }
            if (endDate.HasValue)
            {
                var param = dateSelector.Parameters[0];
                var body = Expression.LessThanOrEqual(dateSelector.Body, Expression.Constant(endDate.Value.Date.AddDays(1)));
                source = source.Where(Expression.Lambda<Func<T, bool>>(body, param));
            }
            return source;
        }

        public static IQueryable<T> WhereTransactionType<T>(this IQueryable<T> source,
                                                            Expression<Func<T, string>> transactionTypeSelector,
                                                            string? transactionType)
        {
            if (!string.IsNullOrEmpty(transactionType))
            {
                var param = transactionTypeSelector.Parameters[0];
                var body = Expression.Equal(transactionTypeSelector.Body, Expression.Constant(transactionType));
                source = source.Where(Expression.Lambda<Func<T, bool>>(body, param));
            }
            return source;
        }
    }
}
