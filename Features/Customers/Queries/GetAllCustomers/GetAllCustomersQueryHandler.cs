using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Customers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Customers.Queries.GetAllCustomers
{
    public class GetAllCustomersQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAllCustomersQueryHandler> _logger
    ) : IRequestHandler<GetAllCustomersQuery, Result>
    {
        public async Task<Result> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.Customers.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    query = query.Where(c =>
                        EF.Functions.ILike(c.Name, term) ||
                        EF.Functions.ILike(c.PhoneNumber, term));
                }

                var pagedResult = await query
                    .OrderBy(c => c.Name)
                    .Select(c => new CustomerResponse(
                        c.Id,
                        c.Group,
                        c.Name,
                        c.Description,
                        c.PhoneNumber,
                        c.Email,
                        c.Address,
                        c.NID,
                        c.OpeningBalance,
                        // Receivable as of the latest ledger row — same definition the sale handler
                        // uses when it computes the running balance (0 when they have no history).
                        c.CustomerTransactions
                            .OrderByDescending(t => t.Id)
                            .Select(t => (decimal?)t.BalanceAfter)
                            .FirstOrDefault() ?? 0,
                        c.CreatedAt))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Customers retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving customers."
                };
            }
        }
    }
}
