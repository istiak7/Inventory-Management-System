using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierPayments
{
    public class GetSupplierPaymentsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetSupplierPaymentsQueryHandler> _logger
    ) : IRequestHandler<GetSupplierPaymentsQuery, Result>
    {
        public async Task<Result> Handle(GetSupplierPaymentsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var payments = await _dbContext.SupplierPayments
                    .AsNoTracking()
                    .Where(p => p.SupplierId == request.SupplierId)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new SupplierPaymentListResponse(
                        p.Id,
                        p.Amount,
                        p.PaymentDate,
                        p.PaymentMethod))
                    .ToListAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Supplier payments retrieved successfully",
                    Data = payments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier payments");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving supplier payments."
                };
            }
        }
    }
}
