using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Customers.Command.DeleteCustomer
{
    // Deletes a customer with no sales, payments or claims (only its opening balance, if any, goes with it).
    public class DeleteCustomerCommandHandler(
        AppDbContext _db,
        ILogger<DeleteCustomerCommandHandler> _logger
    ) : IRequestHandler<DeleteCustomerCommand, Result>
    {
        public async Task<Result> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var entity = await _db.Customers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return Fail(404, "Customer not found.");

            // Checked across every branch (query filters off): a row in another branch still
            // depends on this record.
            if (await _db.CustomerSales.IgnoreQueryFilters().AnyAsync(x => x.CustomerId == id, cancellationToken))
                return Fail(400, "This customer has sales, so it is kept for the sales history.");
            if (await _db.CustomerPayments.IgnoreQueryFilters().AnyAsync(x => x.CustomerId == id, cancellationToken))
                return Fail(400, "This customer has payments, so it is kept for the account history.");
            if (await _db.WarrantyClaims.IgnoreQueryFilters().AnyAsync(x => x.CustomerId == id, cancellationToken))
                return Fail(400, "This customer has warranty claims, so it is kept.");
            if (await _db.Quotations.AnyAsync(x => x.CustomerId == id, cancellationToken))
                return Fail(400, "This customer has price quotations, so it is kept.");
            if (await _db.CustomerTransactions.IgnoreQueryFilters().AnyAsync(x => x.CustomerId == id && x.TransactionType != "Opening", cancellationToken))
                return Fail(400, "This customer has ledger entries, so it is kept for the account history.");

            try
            {
                _db.CustomerTransactions.RemoveRange(_db.CustomerTransactions.IgnoreQueryFilters().Where(t => t.CustomerId == id));
                _db.Customers.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Customer deleted successfully"
                };
            }
            catch (DbUpdateException ex)
            {
                // Something else still points at it (a reference the checks above do not know).
                _logger.LogWarning(ex, "Could not delete Customer {Id}", id);
                return Fail(400, "Customer is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Customer {Id}", id);
                return Fail(500, "An error occurred while deleting the customer.");
            }
        }

        private static Result Fail(int statusCode, string message) => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Status = "Error",
            Message = message
        };
    }
}
