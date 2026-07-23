using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierAccounts
{
    public class GetSupplierAccountsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetSupplierAccountsQueryHandler> _logger
    ) : IRequestHandler<GetSupplierAccountsQuery, Result>
    {
        public async Task<Result> Handle(GetSupplierAccountsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // NOTE: BaseEntity.IsActive defaults to 1, which the EntityStatus enum labels InActive
                // (Active = 0). Records are created with 1 and never flipped to 0, so filtering on
                // "Active" would exclude everything. Exclude only soft-deleted (2) rows instead.
                var query = _dbContext.Suppliers
                    .AsNoTracking()
                    .Where(s => s.IsActive != (int)EntityStatus.Deleted);

                if (request.SupplierId is int supplierId)
                    query = query.Where(s => s.Id == supplierId);

                // Balance = total debited (purchases) - total credited (payments). This equals the
                // running BalanceAfter of the latest transaction, since ledger entries are appended
                // in order. Suppliers with no ledger activity come back with zeros.
                var accounts = await query
                    .OrderBy(s => s.Name)
                    .Select(s => new SupplierAccountResponse(
                        s.Id,
                        s.Name,
                        s.Group,
                        s.SupplierTransactions.Where(t => t.TransactionType == "Purchase").Sum(t => (decimal?)t.Debit) ?? 0m,
                        s.SupplierTransactions.Where(t => t.TransactionType == "Payment").Sum(t => (decimal?)t.Credit) ?? 0m,
                        (s.SupplierTransactions.Sum(t => (decimal?)t.Debit) ?? 0m)
                            - (s.SupplierTransactions.Sum(t => (decimal?)t.Credit) ?? 0m),
                        s.SupplierTransactions
                            .OrderByDescending(t => t.Id)
                            .Select(t => (DateTime?)t.TransactionDate)
                            .FirstOrDefault()))
                    .ToListAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Supplier accounts retrieved successfully",
                    Data = accounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier accounts");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving supplier accounts."
                };
            }
        }
    }
}
