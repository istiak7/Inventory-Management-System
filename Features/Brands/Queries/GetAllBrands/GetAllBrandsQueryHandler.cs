using Inventory_Management_System.Features.Brands.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Queries.GetAllBrands
{
    public class GetAllBrandsQueryHandler(
        IBrandRepository _brandRepository,
        ILogger<GetAllBrandsQueryHandler> _logger
    ) : IRequestHandler<GetAllBrandsQuery, Result>
    {
        public async Task<Result> Handle(GetAllBrandsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pagedResult = await _brandRepository.GetAllPagedAsync(
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Brands retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving brands");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving brands."
                };
            }
        }
    }
}
