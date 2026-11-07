using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Products.Queries.GetProductFilterOptions
{
    public class GetProductFilterOptionsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetProductFilterOptionsQueryHandler> _logger
    ) : IRequestHandler<GetProductFilterOptionsQuery, Result>
    {
        public async Task<Result> Handle(GetProductFilterOptionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var variants = _dbContext.ProductVariants.AsNoTracking();
                if (request.IsSerialized is bool isSerialized)
                    variants = variants.Where(v => v.IsSerialized == isSerialized);

                var forSubCategories = request.BrandId is int brandId
                    ? variants.Where(v => v.Product.BrandId == brandId)
                    : variants;

                var forBrands = request.SubCategoryId is int subCategoryId
                    ? variants.Where(v => v.Product.ProductSubCategoryId == subCategoryId)
                    : variants;

                var subCategories = await forSubCategories
                    .GroupBy(v => new
                    {
                        v.Product.ProductSubCategoryId,
                        v.Product.ProductSubCategories.SubCategoryName,
                        v.Product.ProductSubCategories.ProductCategories.CategoryName
                    })
                    .Select(g => new SubCategoryFilterOption(
                        g.Key.ProductSubCategoryId, g.Key.SubCategoryName, g.Key.CategoryName, g.Count()))
                    .ToListAsync(cancellationToken);

                var brands = await forBrands
                    .GroupBy(v => new { v.Product.BrandId, v.Product.Brand.Name })
                    .Select(g => new BrandFilterOption(g.Key.BrandId, g.Key.Name, g.Count()))
                    .ToListAsync(cancellationToken);

                var response = new GetProductFilterOptionsResponse(
                    subCategories.OrderBy(s => s.CategoryName).ThenBy(s => s.Name).ToList(),
                    brands.OrderBy(b => b.Name).ToList());

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Filter options retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product filter options");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while loading product filters."
                };
            }
        }
    }
}
