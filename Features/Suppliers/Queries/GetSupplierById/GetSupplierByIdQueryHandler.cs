using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Features.Suppliers.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierById
{
    public class GetSupplierByIdQueryHandler(
        ISupplierRepository _supplierRepository,
        ILogger<GetSupplierByIdQueryHandler> _logger
    ) : IRequestHandler<GetSupplierByIdQuery, Result>
    {
        public async Task<Result> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var supplier = await _supplierRepository.GetByIdAsync(request.Id, cancellationToken);

                if (supplier is null)
                {
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Not Found",
                        Message = $"Supplier with id {request.Id} was not found."
                    };
                }

                var response = new SupplierResponse(
                    supplier.Id,
                    supplier.Group,
                    supplier.Name,
                    supplier.Description,
                    supplier.PhoneNumber,
                    supplier.Email,
                    supplier.NID,
                    supplier.OpeningBalance,
                    supplier.CreatedAt);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Supplier retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier with id {Id}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the supplier."
                };
            }
        }
    }
}
