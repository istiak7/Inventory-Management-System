using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Features.Customers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerByPhone
{
    public class GetCustomerByPhoneQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetCustomerByPhoneQueryHandler> _logger
    ) : IRequestHandler<GetCustomerByPhoneQuery, Result>
    {
        public async Task<Result> Handle(GetCustomerByPhoneQuery request, CancellationToken cancellationToken)
        {
            var phoneNumber = CustomerPhoneNumber.Normalize(request.PhoneNumber);

            if (phoneNumber.Length < CustomerPhoneNumber.MinimumDigits)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"Phone number must contain at least {CustomerPhoneNumber.MinimumDigits} digits."
                };
            }

            try
            {
                var customer = await _dbContext.Customers
                    .AsNoTracking()
                    .Where(c => c.PhoneNumber == phoneNumber)
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
                        // Same definition GetAllCustomers and the sale handler use: the receivable
                        // as of the latest ledger row, so the till can see what is already owed
                        // before agreeing to another credit sale.
                        c.CustomerTransactions
                            .OrderByDescending(t => t.Id)
                            .Select(t => (decimal?)t.BalanceAfter)
                            .FirstOrDefault() ?? 0,
                        c.CreatedAt))
                    .FirstOrDefaultAsync(cancellationToken);

                // "Nobody has this number" is a successful answer, not a failure — it is the
                // branch that tells the sales form to offer customer creation. Reporting it as an
                // error would make the client show a toast on every new walk-in.
                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = customer == null
                        ? "No customer found with this phone number."
                        : "Customer retrieved successfully",
                    Data = customer
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer by phone number");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the customer."
                };
            }
        }
    }
}
