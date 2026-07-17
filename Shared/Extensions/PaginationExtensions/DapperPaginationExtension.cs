using Dapper;
using System.Data;

namespace Inventory_Management_System.Shared.Extensions.PaginationExtensions
{
    public static class DapperPaginationExtension
    {
        // Batches the count and the page into a single round trip: Dapper sends both
        // statements together and reads two result sets back from one command.
        public static async Task<PagedResult<T>> QueryPagedAsync<T>(
            this IDbConnection connection,
            string sql,
            object? parameters = null,
            int pageNumber = 1,
            int pageSize = 20,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var offset = (pageNumber - 1) * pageSize;

            var batchedSql = $@"
                SELECT COUNT(*) FROM ({sql}) AS ""__CountQuery"";
                {sql} LIMIT @_PageSize OFFSET @_Offset;";

            var dynamicParameters = new DynamicParameters(parameters);
            dynamicParameters.Add("__PageSize", pageSize);
            dynamicParameters.Add("__Offset", offset);

            var command = new CommandDefinition(
                batchedSql,
                dynamicParameters,
                transaction,
                commandTimeout,
                cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);

            var totalCount = await multi.ReadSingleAsync<long>();
            var items = (await multi.ReadAsync<T>()).AsList();

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
