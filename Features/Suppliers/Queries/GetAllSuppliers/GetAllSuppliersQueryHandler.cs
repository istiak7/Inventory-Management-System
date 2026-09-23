using Inventory_Management_System.Features.Suppliers.Shared;
using Inventory_Management_System.Features.Suppliers.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetAllSuppliers
{
    public class GetAllSuppliersQueryHandler(
        ISupplierRepository _supplierRepository,
        ILogger<GetAllSuppliersQueryHandler> _logger
    ) : IRequestHandler<GetAllSuppliersQuery, Result>
    {
        public async Task<Result> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pagedResult = await _supplierRepository.GetAllPagedAsync(
                    request.PageNumber,
                    request.PageSize,
                    request.Search,
                    cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Suppliers retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving suppliers."
                };
            }
        }
    }
}
