using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Customers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerAccounts
{
    public class GetCustomerAccountsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetCustomerAccountsQueryHandler> _logger
    ) : IRequestHandler<GetCustomerAccountsQuery, Result>
    {
        public async Task<Result> Handle(GetCustomerAccountsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // NOTE: BaseEntity.IsActive defaults to 1, which the EntityStatus enum labels InActive
                // (Active = 0). Records are created with 1 and never flipped to 0, so filtering on
                // "Active" would exclude everything. Exclude only soft-deleted (2) rows instead.
                var query = _dbContext.Customers
                    .AsNoTracking()
                    .Where(c => c.IsActive != (int)EntityStatus.Deleted);

                if (request.CustomerId is int customerId)
                    query = query.Where(c => c.Id == customerId);

                // CustomerTransaction uses the OPPOSITE Debit/Credit convention from
                // SupplierTransaction (see the entity's own comment): Sale -> Credit (balance up,
                // they owe more), Payment -> Debit (balance down, they owe less). Balance = total
                // credited (sales) - total debited (payments). This equals the running BalanceAfter
                // of the latest transaction, since ledger entries are appended in order. Customers
                // with no ledger activity come back with zeros.
                var accounts = await query
                    .OrderBy(c => c.Name)
                    .Select(c => new CustomerAccountResponse(
                        c.Id,
                        c.Name,
                        c.Group,
                        c.CustomerTransactions.Where(t => t.TransactionType == "Sale").Sum(t => (decimal?)t.Credit) ?? 0m,
                        c.CustomerTransactions.Where(t => t.TransactionType == "Payment").Sum(t => (decimal?)t.Debit) ?? 0m,
                        (c.CustomerTransactions.Sum(t => (decimal?)t.Credit) ?? 0m)
                            - (c.CustomerTransactions.Sum(t => (decimal?)t.Debit) ?? 0m),
                        c.CustomerTransactions
                            .OrderByDescending(t => t.Id)
                            .Select(t => (DateTime?)t.TransactionDate)
                            .FirstOrDefault(),
                        c.PhoneNumber,
                        c.Email,
                        c.Address,
                        c.OpeningBalance))
                    .ToListAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Customer accounts retrieved successfully",
                    Data = accounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer accounts");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving customer accounts."
                };
            }
        }
    }
}
