using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Inventory.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Inventory.Queries.GetAllInventoryTransactions
{
    public class GetAllInventoryTransactionsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAllInventoryTransactionsQueryHandler> _logger
    ) : IRequestHandler<GetAllInventoryTransactionsQuery, Result>
    {
        public async Task<Result> Handle(GetAllInventoryTransactionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.InventoryTransactions.AsNoTracking();

                if (request.BranchId is int branchId)
                    query = query.Where(t => t.BranchId == branchId);

                if (request.ProductVariantId is int variantId)
                    query = query.Where(t => t.ProductVariantId == variantId);

                if (!string.IsNullOrWhiteSpace(request.TransactionType) &&
                    Enum.TryParse<InventoryTxnType>(request.TransactionType, true, out var typeFilter))
                    query = query.Where(t => t.TransactionType == typeFilter);

                // Materialize with the enum intact, then map to a string DTO in memory (newest first).
                var paged = await query
                    .OrderByDescending(t => t.Id)
                    .Select(t => new
                    {
                        t.Id,
                        t.BranchId,
                        BranchName = t.Branch.Name,
                        t.ProductVariantId,
                        Sku = t.ProductVariant.SKU,
                        ProductName = t.ProductVariant.Product.ProductName,
                        t.TransactionType,
                        t.QuantityIn,
                        t.QuantityOut,
                        t.BalanceAfter,
                        t.TransactionDate,
                        t.SupplierPurchaseDetailsId,
                        InvoiceNumber = t.SupplierPurchaseDetails != null
                            ? t.SupplierPurchaseDetails.SupplierPurchase.InvoiceNumber
                            : null
                    })
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                var items = paged.Items.Select(t => new InventoryTransactionResponse(
                    t.Id, t.BranchId, t.BranchName, t.ProductVariantId, t.Sku, t.ProductName,
                    t.TransactionType.ToString(), t.QuantityIn, t.QuantityOut, t.BalanceAfter,
                    t.TransactionDate, t.SupplierPurchaseDetailsId, t.InvoiceNumber)).ToList();

                var pagedResult = new PagedResult<InventoryTransactionResponse>
                {
                    Items = items,
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalCount = paged.TotalCount
                };

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Inventory transactions retrieved successfully", Data = pagedResult };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inventory transactions");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving inventory transactions." };
            }
        }
    }
}
