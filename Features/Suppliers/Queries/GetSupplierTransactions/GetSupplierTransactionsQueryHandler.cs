using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierTransactions
{
    public class GetSupplierTransactionsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetSupplierTransactionsQueryHandler> _logger
    ) : IRequestHandler<GetSupplierTransactionsQuery, Result>
    {
        public async Task<Result> Handle(GetSupplierTransactionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.SupplierTransactions.AsNoTracking();

                if (request.SupplierId is int supplierId)
                    query = query.Where(t => t.SupplierId == supplierId);

                var pagedResult = await query
                    .OrderByDescending(t => t.Id)
                    .Select(t => new SupplierLedgerEntryResponse(
                        t.Id,
                        t.SupplierId,
                        t.Supplier.Name,
                        t.TransactionType,
                        t.TransactionDate,
                        // Reference: invoice number for purchases, PAY-x for payments, else TXN-x.
                        t.SupplierPurchaseId != null
                            ? (t.SupplierPurchase!.InvoiceNumber ?? ("PUR-" + t.SupplierPurchaseId))
                            : t.SupplierPaymentId != null
                                ? ("PAY-" + t.SupplierPaymentId)
                                : ("TXN-" + t.Id),
                        t.Debit,
                        t.Credit,
                        t.BalanceAfter))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Supplier transactions retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier transactions");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving supplier transactions."
                };
            }
        }
    }
}
