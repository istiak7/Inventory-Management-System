using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;
using static Inventory_Management_System.Entities.Common.EntityConstant;
using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Features.Parties.Shared;

namespace Inventory_Management_System.Features.Suppliers.Command.CreateSuppliers
{
    public class CreateSupplierCommandHandler(
        IBaseRepository<Supplier> _supplierRepository,
        AppDbContext _db,
        ILogger<CreateSupplierCommandHandler> _logger
    ) : IRequestHandler<CreateSupplierCommand, Result>

    {
        public async Task<Result> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
        {
            // The phone number links a supplier to its customer row, so each supplier has its own.
            var phoneNumber = CustomerPhoneNumber.Normalize(request.PhoneNumber);
            var existingSupplier = await _supplierRepository.GetAsync(s => s.PhoneNumber == phoneNumber
                                            && s.IsActive != (int)EntityStatus.Deleted, cancellationToken: cancellationToken);
            if (existingSupplier != null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"Supplier '{existingSupplier.Name}' already has this phone number."
                };
            }

            try
            {
                var supplier = new Supplier
                {
                    Group = request.Group,
                    Name = request.Name,
                    Description = request.Description,
                    PhoneNumber = phoneNumber,
                    Email = request.Email?.Trim() ?? string.Empty,
                    Address = request.Address?.Trim() ?? string.Empty,
                    NID = request.NID,
                    OpeningBalance = request.OpeningBalance,
                    CreatedAt = DateTime.UtcNow,
                };

                // The opening balance goes into the ledger, so the balance, the payment limit
                // and every statement include it.
                if (supplier.OpeningBalance != 0)
                    supplier.SupplierTransactions.Add(SupplierTransaction.ForOpening(supplier));

                // Every supplier is a credit party: it gets a linked customer row (found by
                // phone number, or created), so sales and purchases can settle each other.
                var (_, linkError) = await new PartyLink(_db).EnsureCustomerForSupplierAsync(supplier, cancellationToken);
                if (linkError != null)
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = linkError };
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
