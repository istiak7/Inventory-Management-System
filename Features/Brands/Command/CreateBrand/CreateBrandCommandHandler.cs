using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Brands.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Command.CreateBrand
{
    public class CreateBrandCommandHandler(
        IBrandRepository _brandRepository,
        ILogger<CreateBrandCommandHandler> _logger
    ) : IRequestHandler<CreateBrandCommand, Result>
    {
        public async Task<Result> Handle(
            CreateBrandCommand request,
            CancellationToken cancellationToken)
        {
            var existing = await _brandRepository.GetAsync(b => b.Name == request.Name);

            if (existing is not null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"A brand with name '{request.Name}' already exists."
                };
            }

            try
            {
                var brand = new Brand
                {
                    Name = request.Name,
                    Description = request.Description,
                    LogoUrl = request.LogoUrl,
                    CreatedAt = DateTime.UtcNow
                };

                await _brandRepository.AddAsync(brand, cancellationToken);
                await _brandRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Brand created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating brand");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the brand."
                };
            }
        }
    }
}
