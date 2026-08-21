using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Products.Queries.SearchProducts
{
    public class SearchProductsQueryHandler(
        AppDbContext _dbContext,
        ILogger<SearchProductsQueryHandler> _logger
    ) : IRequestHandler<SearchProductsQuery, Result>
    {
        private const string SearchConfig = "english";

        public async Task<Result> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pageNumber = Math.Max(request.PageNumber, 1);
                var pageSize = Math.Clamp(request.PageSize, 1, SearchProductsValidator.MaxPageSize);

                var term = request.Term?.Trim();
                if (term?.Length > SearchProductsValidator.MaxTermLength)
                    term = term[..SearchProductsValidator.MaxTermLength];

                var query = _dbContext.ProductVariants.AsNoTracking();

                if (request.ProductId is int productId)
                    query = query.Where(v => v.ProductId == productId);

                var branchId = request.BranchId;
                if (branchId is not null)
                    query = query.Where(v => v.Stocks.Any(s => s.BranchId == branchId));

                if (request.IsSerialized is bool isSerialized)
                    query = query.Where(v => v.IsSerialized == isSerialized);

                if (request.MinPrice is decimal minPrice)
                    query = query.Where(v => v.SellingPrice >= minPrice);

                if (request.MaxPrice is decimal maxPrice)
                    query = query.Where(v => v.SellingPrice <= maxPrice);

                IQueryable<SearchProductsResponse> projected;

                if (string.IsNullOrWhiteSpace(term))
                {
                    projected = query
                        .OrderBy(v => v.ProductId).ThenBy(v => v.Id)
                        .Select(v => new SearchProductsResponse(
                            v.Id, v.ProductId, v.Product.ProductName, v.SKU, v.Barcode,
                            v.SellingPrice, v.IsSerialized, v.AttributesJson, 0f,
                            branchId == null
                                ? null
                                : v.Stocks.Where(s => s.BranchId == branchId)
                                          .Select(s => (int?)s.CurrentStock)
                                          .FirstOrDefault() ?? 0));
                }
                else
                {
                    var prefix = $"{EscapeLikePattern(term)}%";

                    projected = query
                        .Where(v => v.SearchVector.Matches(EF.Functions.WebSearchToTsQuery(SearchConfig, term))
                                 || EF.Functions.ILike(v.Product.ProductName, prefix, @"\")
                                 || EF.Functions.ILike(v.SKU, prefix, @"\")
                                 || EF.Functions.ILike(v.Barcode, prefix, @"\"))
                        .OrderByDescending(v => v.SearchVector.Rank(
                            EF.Functions.WebSearchToTsQuery(SearchConfig, term)))
                        .ThenBy(v => v.Id)
                        .Select(v => new SearchProductsResponse(
                            v.Id, v.ProductId, v.Product.ProductName, v.SKU, v.Barcode,
                            v.SellingPrice, v.IsSerialized, v.AttributesJson,
                            v.SearchVector.Rank(EF.Functions.WebSearchToTsQuery(SearchConfig, term)),
                            branchId == null
                                ? null
                                : v.Stocks.Where(s => s.BranchId == branchId)
                                          .Select(s => (int?)s.CurrentStock)
                                          .FirstOrDefault() ?? 0));
                }

                var pagedResult = await projected.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Products retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while searching products."
                };
            }
        }

        private static string EscapeLikePattern(string value) =>
            value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
    }
}
