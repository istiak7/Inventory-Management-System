using Inventory_Management_System.Features.Suppliers.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Command.UpdateSupplier
{
    public class UpdateSupplierCommandHandler(
        ISupplierRepository _supplierRepository,
        ILogger<UpdateSupplierCommandHandler> _logger
    ) : IRequestHandler<UpdateSupplierCommand, Result>
    {
        public async Task<Result> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
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

            try
            {
                supplier.Group = request.Group;
                supplier.Name = request.Name;
                supplier.Description = request.Description;
                supplier.PhoneNumber = request.PhoneNumber;
                supplier.Email = request.Email;
                supplier.NID = request.NID;
                supplier.OpeningBalance = request.OpeningBalance;
                supplier.UpDatedAt = DateTime.UtcNow;

                await _supplierRepository.UpdateAsync(supplier, cancellationToken);
                await _supplierRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Supplier updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier with id {Id}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while updating the supplier."
                };
            }
        }
    }
}
