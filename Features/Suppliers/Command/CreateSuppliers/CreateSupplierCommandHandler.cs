using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Suppliers.Command.CreateSuppliers
{
    public class CreateSupplierCommandHandler(
        IBaseRepository<Supplier> _supplierRepository,
        ILogger<CreateSupplierCommandHandler> _logger
    ) : IRequestHandler<CreateSupplierCommand, Result>

    {
        public async Task<Result> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
        {
            var existingSupplier = await _supplierRepository.GetAsync(s => s.Name == request.Name && s.PhoneNumber == request.PhoneNumber
                                            && s.IsActive != (int)EntityStatus.Deleted, cancellationToken: cancellationToken);

            if (existingSupplier != null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Supplier already exists"
                };
            }
            try
            {
                var supplier = new Supplier
                {
                    Group = request.Group,
                    Name = request.Name,
                    Description = request.Description,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    NID = request.NID,
                    OpeningBalance = request.OpeningBalance,
                    CreatedAt = DateTime.UtcNow,
                };

                // The opening balance goes into the ledger, so the balance, the payment limit
                // and every statement include it.
                if (supplier.OpeningBalance != 0)
                    supplier.SupplierTransactions.Add(SupplierTransaction.ForOpening(supplier));
                await _supplierRepository.AddAsync(supplier, cancellationToken);
                await _supplierRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Supplier created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the supplier."
                };
            }
        }
    }
}
