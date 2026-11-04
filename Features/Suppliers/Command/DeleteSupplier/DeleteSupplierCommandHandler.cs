using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Suppliers.Command.DeleteSupplier
{
    // Deletes a supplier with no purchases or payments (only its opening balance, if any, goes with it).
    public class DeleteSupplierCommandHandler(
        AppDbContext _db,
        ILogger<DeleteSupplierCommandHandler> _logger
    ) : IRequestHandler<DeleteSupplierCommand, Result>
    {
        public async Task<Result> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var entity = await _db.Suppliers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return Fail(404, "Supplier not found.");

            // Checked across every branch (query filters off): a row in another branch still
            // depends on this record.
            if (await _db.SupplierPurchases.IgnoreQueryFilters().AnyAsync(x => x.SupplierId == id, cancellationToken))
                return Fail(400, "This supplier has purchase orders, so it is kept for the purchase history.");
            if (await _db.SupplierPayments.IgnoreQueryFilters().AnyAsync(x => x.SupplierId == id, cancellationToken))
                return Fail(400, "This supplier has payments, so it is kept for the account history.");
            if (await _db.SupplierTransactions.IgnoreQueryFilters().AnyAsync(x => x.SupplierId == id && x.TransactionType != "Opening", cancellationToken))
                return Fail(400, "This supplier has ledger entries, so it is kept for the account history.");

            try
            {
                _db.SupplierTransactions.RemoveRange(_db.SupplierTransactions.IgnoreQueryFilters().Where(t => t.SupplierId == id));
                _db.Suppliers.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Supplier deleted successfully"
                };
            }
            catch (DbUpdateException ex)
            {
                // Something else still points at it (a reference the checks above do not know).
                _logger.LogWarning(ex, "Could not delete Supplier {Id}", id);
                return Fail(400, "Supplier is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Supplier {Id}", id);
                return Fail(500, "An error occurred while deleting the supplier.");
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
