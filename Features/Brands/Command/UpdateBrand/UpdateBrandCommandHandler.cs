using Inventory_Management_System.Features.Brands.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Command.UpdateBrand
{
    public class UpdateBrandCommandHandler(
        IBrandRepository _brandRepository,
        ILogger<UpdateBrandCommandHandler> _logger
    ) : IRequestHandler<UpdateBrandCommand, Result>
    {
        public async Task<Result> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
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

            try
            {
                brand.Name = request.Name;
                brand.Description = request.Description;
                brand.LogoUrl = request.LogoUrl;
                brand.UpDatedAt = DateTime.UtcNow;

                await _brandRepository.UpdateAsync(brand, cancellationToken);
                await _brandRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Brand updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating brand with id {Id}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while updating the brand."
                };
            }
        }
    }
}
