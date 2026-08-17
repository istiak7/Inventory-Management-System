using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Customers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerPayments
{
    public class GetCustomerPaymentsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetCustomerPaymentsQueryHandler> _logger
    ) : IRequestHandler<GetCustomerPaymentsQuery, Result>
    {
        public async Task<Result> Handle(GetCustomerPaymentsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var payments = await _dbContext.CustomerPayments
                    .AsNoTracking()
                    .Where(p => p.CustomerId == request.CustomerId)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new CustomerPaymentListResponse(
                        p.Id,
                        p.Amount,
                        p.PaymentDate,
                        p.PaymentMethod,
                        p.Remarks))
                    .ToListAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Customer payments retrieved successfully",
                    Data = payments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer payments");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving customer payments."
                };
            }
        }
    }
}
