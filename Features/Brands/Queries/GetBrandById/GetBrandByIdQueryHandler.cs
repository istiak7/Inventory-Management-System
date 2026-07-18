using Inventory_Management_System.Features.Brands.Shared.Dtos;
using Inventory_Management_System.Features.Brands.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Queries.GetBrandById
{
    public class GetBrandByIdQueryHandler(
        IBrandRepository _brandRepository,
        ILogger<GetBrandByIdQueryHandler> _logger
    ) : IRequestHandler<GetBrandByIdQuery, Result>
    {
        public async Task<Result> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var brand = await _brandRepository.GetByIdAsync(request.Id, cancellationToken);

                if (brand is null)
                {
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Not Found",
                        Message = $"Brand with id {request.Id} was not found."
                    };
                }

                var response = new BrandResponse(
                    brand.Id,
                    brand.Name,
                    brand.Description,
                    brand.LogoUrl,
                    brand.CreatedAt);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Brand retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving brand with id {Id}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the brand."
                };
            }
        }
    }
}
