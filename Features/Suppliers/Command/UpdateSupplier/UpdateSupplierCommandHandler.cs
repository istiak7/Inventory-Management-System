using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Suppliers.Shared.Repository;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Command.UpdateSupplier
{
    public class UpdateSupplierCommandHandler(
        ISupplierRepository _supplierRepository,
        AppDbContext _dbContext,
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

            // Changing the opening balance after purchases or payments would make every running
            // balance after it wrong, so it is only allowed while the ledger is still empty.
            List<SupplierTransaction> openingRows = [];
            if (request.OpeningBalance != supplier.OpeningBalance)
            {
                var hasOtherTransactions = await _dbContext.SupplierTransactions
                    .AnyAsync(t => t.SupplierId == supplier.Id && t.TransactionType != "Opening", cancellationToken);
                if (hasOtherTransactions)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = "The opening balance cannot be changed after purchases or payments exist for this supplier."
                    };

                openingRows = await _dbContext.SupplierTransactions
                    .Where(t => t.SupplierId == supplier.Id && t.TransactionType == "Opening")
                    .ToListAsync(cancellationToken);
            }

            try
            {
                if (request.OpeningBalance != supplier.OpeningBalance)
                {
                    _dbContext.SupplierTransactions.RemoveRange(openingRows);
                    supplier.OpeningBalance = request.OpeningBalance;
                    if (supplier.OpeningBalance != 0)
                        await _dbContext.SupplierTransactions.AddAsync(SupplierTransaction.ForOpening(supplier), cancellationToken);
                }

                supplier.Group = request.Group;
                supplier.Name = request.Name;
                supplier.Description = request.Description;
                supplier.PhoneNumber = request.PhoneNumber;
                supplier.Email = request.Email;
                supplier.NID = request.NID;

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
