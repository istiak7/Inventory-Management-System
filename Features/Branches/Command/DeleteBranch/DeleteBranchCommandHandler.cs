using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Branches.Command.DeleteBranch
{
    // Deletes a warehouse (branch) nothing has happened in yet.
    public class DeleteBranchCommandHandler(
        AppDbContext _db,
        ILogger<DeleteBranchCommandHandler> _logger
    ) : IRequestHandler<DeleteBranchCommand, Result>
    {
        public async Task<Result> Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var entity = await _db.Branches.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return Fail(404, "Warehouse not found.");

            // Checked across every branch (query filters off): a row in another branch still
            // depends on this record.
            if (await _db.Users.AnyAsync(x => x.BranchId == id, cancellationToken))
                return Fail(400, "Users are assigned to this warehouse. Move them first.");
            if (await _db.Stocks.IgnoreQueryFilters().AnyAsync(x => x.BranchId == id, cancellationToken))
                return Fail(400, "This warehouse has stock records, so it is kept.");
            if (await _db.InventoryTransactions.IgnoreQueryFilters().AnyAsync(x => x.BranchId == id, cancellationToken))
                return Fail(400, "This warehouse has stock movements, so it is kept.");
            if (await _db.ProductSerials.IgnoreQueryFilters().AnyAsync(x => x.BranchId == id, cancellationToken))
                return Fail(400, "This warehouse has serial numbers recorded, so it is kept.");
            if (await _db.SupplierPurchases.IgnoreQueryFilters().AnyAsync(x => x.BranchId == id, cancellationToken))
                return Fail(400, "This warehouse has purchase orders, so it is kept.");
            if (await _db.SupplierPayments.IgnoreQueryFilters().AnyAsync(x => x.BranchId == id, cancellationToken))
                return Fail(400, "This warehouse has supplier payments, so it is kept.");
            if (await _db.CustomerSales.IgnoreQueryFilters().AnyAsync(x => x.BranchId == id, cancellationToken))
                return Fail(400, "This warehouse has sales, so it is kept.");
            if (await _db.CustomerPayments.IgnoreQueryFilters().AnyAsync(x => x.BranchId == id, cancellationToken))
                return Fail(400, "This warehouse has customer payments, so it is kept.");
            if (await _db.StockTransfers.IgnoreQueryFilters().AnyAsync(x => x.SourceBranchId == id || x.DestinationBranchId == id, cancellationToken))
                return Fail(400, "This warehouse has stock transfers, so it is kept.");
            if (await _db.WarrantyClaims.IgnoreQueryFilters().AnyAsync(x => x.BranchId == id, cancellationToken))
                return Fail(400, "This warehouse has warranty claims, so it is kept.");
            if (await _db.Expenses.IgnoreQueryFilters().AnyAsync(x => x.BranchId == id, cancellationToken))
                return Fail(400, "This warehouse has office expenses, so it is kept.");

            try
            {
                _db.Branches.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Warehouse deleted successfully"
                };
            }
            catch (DbUpdateException ex)
            {
                // Something else still points at it (a reference the checks above do not know).
                _logger.LogWarning(ex, "Could not delete Warehouse {Id}", id);
                return Fail(400, "Warehouse is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Warehouse {Id}", id);
                return Fail(500, "An error occurred while deleting the warehouse.");
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
