using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Reports.Queries.GetStockDateWiseReport
{
    public class GetStockDateWiseReportQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetStockDateWiseReportQueryHandler> _logger
    ) : IRequestHandler<GetStockDateWiseReportQuery, Result>
    {
        public async Task<Result> Handle(GetStockDateWiseReportQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var (pageNumber, pageSize) = ReportPaging.Normalize(request.PageNumber, request.PageSize);
                var range = ReportDateRange.From(request.StartDate, request.EndDate);

                var rangeStart = range.Start ?? DateTime.MinValue;
                var rangeEnd = range.EndExclusive ?? DateTime.MaxValue;

                var stocks = ApplyStockFilters(_dbContext.Stocks.AsNoTracking(), request);
                var movements = ApplyMovementFilters(_dbContext.InventoryTransactions.AsNoTracking(), request);

                var totalCount = await stocks.LongCountAsync(cancellationToken);

                var ordered = ApplySort(stocks, request, rangeStart, rangeEnd);

                var rawRows = await ordered
                    .Skip(ReportPaging.SkipCount(pageNumber, pageSize))
                    .Take(pageSize)
                    .Select(s => new
                    {
                        s.BranchId,
                        BranchName = s.Branch.Name,
                        s.ProductVariantId,
                        Sku = s.ProductVariant.SKU,
                        ProductName = s.ProductVariant.Product.ProductName,
                        CategoryName = s.ProductVariant.Product.ProductSubCategories.ProductCategories.CategoryName,
                        SubCategoryName = s.ProductVariant.Product.ProductSubCategories.SubCategoryName,
                        Opening = s.CurrentStock - (_dbContext.InventoryTransactions
                            .Where(t => t.BranchId == s.BranchId
                                     && t.ProductVariantId == s.ProductVariantId
                                     && t.TransactionDate >= rangeStart)
                            .Sum(t => (int?)(t.QuantityIn - t.QuantityOut)) ?? 0),
                        PurchaseIn = _dbContext.InventoryTransactions
                            .Where(t => t.BranchId == s.BranchId
                                     && t.ProductVariantId == s.ProductVariantId
                                     && t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                     && t.TransactionType == InventoryTxnType.PurchaseIn)
                            .Sum(t => (int?)t.QuantityIn) ?? 0,
                        ReturnIn = _dbContext.InventoryTransactions
                            .Where(t => t.BranchId == s.BranchId
                                     && t.ProductVariantId == s.ProductVariantId
                                     && t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                     && t.TransactionType == InventoryTxnType.ReturnIn)
                            .Sum(t => (int?)t.QuantityIn) ?? 0,
                        TransferIn = _dbContext.InventoryTransactions
                            .Where(t => t.BranchId == s.BranchId
                                     && t.ProductVariantId == s.ProductVariantId
                                     && t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                     && t.TransactionType == InventoryTxnType.TransferIn)
                            .Sum(t => (int?)t.QuantityIn) ?? 0,
                        SaleOut = _dbContext.InventoryTransactions
                            .Where(t => t.BranchId == s.BranchId
                                     && t.ProductVariantId == s.ProductVariantId
                                     && t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                     && t.TransactionType == InventoryTxnType.SaleOut)
                            .Sum(t => (int?)t.QuantityOut) ?? 0,
                        ReturnOut = _dbContext.InventoryTransactions
                            .Where(t => t.BranchId == s.BranchId
                                     && t.ProductVariantId == s.ProductVariantId
                                     && t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                     && t.TransactionType == InventoryTxnType.ReturnOut)
                            .Sum(t => (int?)t.QuantityOut) ?? 0,
                        TransferOut = _dbContext.InventoryTransactions
                            .Where(t => t.BranchId == s.BranchId
                                     && t.ProductVariantId == s.ProductVariantId
                                     && t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                     && t.TransactionType == InventoryTxnType.TransferOut)
                            .Sum(t => (int?)t.QuantityOut) ?? 0,
                        DamageOut = _dbContext.InventoryTransactions
                            .Where(t => t.BranchId == s.BranchId
                                     && t.ProductVariantId == s.ProductVariantId
                                     && t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                     && t.TransactionType == InventoryTxnType.DamageOut)
                            .Sum(t => (int?)t.QuantityOut) ?? 0,
                        Adjustment = _dbContext.InventoryTransactions
                            .Where(t => t.BranchId == s.BranchId
                                     && t.ProductVariantId == s.ProductVariantId
                                     && t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                     && t.TransactionType == InventoryTxnType.Adjustment)
                            .Sum(t => (int?)(t.QuantityIn - t.QuantityOut)) ?? 0,
                    })
                    .ToListAsync(cancellationToken);

                var rows = rawRows
                    .Select(r =>
                    {
                        var stockIn = r.PurchaseIn + r.ReturnIn + r.TransferIn;
                        var stockOut = r.SaleOut + r.ReturnOut + r.TransferOut + r.DamageOut;

                        return new StockDateWiseRow(
                            r.BranchId,
                            r.BranchName,
                            r.ProductVariantId,
                            r.Sku,
                            r.ProductName,
                            r.CategoryName,
                            r.SubCategoryName,
                            r.Opening,
                            r.PurchaseIn,
                            r.ReturnIn,
                            r.TransferIn,
                            r.SaleOut,
                            r.ReturnOut,
                            r.TransferOut,
                            r.DamageOut,
                            r.Adjustment,
                            stockIn,
                            stockOut,
                            r.Opening + stockIn - stockOut + r.Adjustment);
                    })
                    .ToList();

                var totalCurrentStock = await stocks.SumAsync(s => s.CurrentStock, cancellationToken);

                var netFromRangeStart = await movements
                    .SumAsync(t => t.TransactionDate >= rangeStart ? t.QuantityIn - t.QuantityOut : 0, cancellationToken);

                var totalOpening = totalCurrentStock - netFromRangeStart;

                var totalStockIn = await movements
                    .SumAsync(t => t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                && (t.TransactionType == InventoryTxnType.PurchaseIn
                                 || t.TransactionType == InventoryTxnType.ReturnIn
                                 || t.TransactionType == InventoryTxnType.TransferIn)
                        ? t.QuantityIn : 0, cancellationToken);

                var totalStockOut = await movements
                    .SumAsync(t => t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                && (t.TransactionType == InventoryTxnType.SaleOut
                                 || t.TransactionType == InventoryTxnType.ReturnOut
                                 || t.TransactionType == InventoryTxnType.TransferOut
                                 || t.TransactionType == InventoryTxnType.DamageOut)
                        ? t.QuantityOut : 0, cancellationToken);

                var totalAdjustment = await movements
                    .SumAsync(t => t.TransactionDate >= rangeStart && t.TransactionDate < rangeEnd
                                && t.TransactionType == InventoryTxnType.Adjustment
                        ? t.QuantityIn - t.QuantityOut : 0, cancellationToken);

                var summary = new StockDateWiseSummary(
                    totalCount,
                    totalOpening,
                    totalStockIn,
                    totalStockOut,
                    totalAdjustment,
                    totalOpening + totalStockIn - totalStockOut + totalAdjustment);

                var response = new StockDateWiseReportResponse(
                    summary,
                    ReportPaging.Page(rows, totalCount, pageNumber, pageSize));

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Stock date wise report retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving the stock date wise report");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the stock date wise report."
                };
            }
        }

        private static IQueryable<Stock> ApplyStockFilters(IQueryable<Stock> query, GetStockDateWiseReportQuery request)
        {
            if (request.BranchId is int branchId)
                query = query.Where(s => s.BranchId == branchId);

            if (request.ProductVariantId is int variantId)
                query = query.Where(s => s.ProductVariantId == variantId);

            if (request.ProductId is int productId)
                query = query.Where(s => s.ProductVariant.ProductId == productId);

            if (request.SubCategoryId is int subCategoryId)
                query = query.Where(s => s.ProductVariant.Product.ProductSubCategoryId == subCategoryId);

            if (request.CategoryId is int categoryId)
                query = query.Where(s => s.ProductVariant.Product.ProductSubCategories.ProductCategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = $"%{request.Search.Trim()}%";
                query = query.Where(s =>
                    EF.Functions.ILike(s.ProductVariant.SKU, term) ||
                    EF.Functions.ILike(s.ProductVariant.Barcode, term) ||
                    EF.Functions.ILike(s.ProductVariant.Product.ProductName, term));
            }

            return query;
        }

        private static IQueryable<InventoryTransaction> ApplyMovementFilters(
            IQueryable<InventoryTransaction> query,
            GetStockDateWiseReportQuery request)
        {
            if (request.BranchId is int branchId)
                query = query.Where(t => t.BranchId == branchId);

            if (request.ProductVariantId is int variantId)
                query = query.Where(t => t.ProductVariantId == variantId);

            if (request.ProductId is int productId)
                query = query.Where(t => t.ProductVariant.ProductId == productId);

            if (request.SubCategoryId is int subCategoryId)
                query = query.Where(t => t.ProductVariant.Product.ProductSubCategoryId == subCategoryId);

            if (request.CategoryId is int categoryId)
                query = query.Where(t => t.ProductVariant.Product.ProductSubCategories.ProductCategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = $"%{request.Search.Trim()}%";
                query = query.Where(t =>
                    EF.Functions.ILike(t.ProductVariant.SKU, term) ||
                    EF.Functions.ILike(t.ProductVariant.Barcode, term) ||
                    EF.Functions.ILike(t.ProductVariant.Product.ProductName, term));
            }

            return query;
        }

        private IQueryable<Stock> ApplySort(
            IQueryable<Stock> query,
            GetStockDateWiseReportQuery request,
            DateTime rangeStart,
            DateTime rangeEnd)
        {
            var descending = request.SortDescending;

            IOrderedQueryable<Stock> ordered = request.SortBy?.Trim().ToLowerInvariant() switch
            {
                "sku" => descending
                    ? query.OrderByDescending(s => s.ProductVariant.SKU)
                    : query.OrderBy(s => s.ProductVariant.SKU),

                "branch" => descending
                    ? query.OrderByDescending(s => s.Branch.Name)
                    : query.OrderBy(s => s.Branch.Name),

                "opening" => descending
                    ? query.OrderByDescending(s => s.CurrentStock - (_dbContext.InventoryTransactions
                        .Where(t => t.BranchId == s.BranchId
                                 && t.ProductVariantId == s.ProductVariantId
                                 && t.TransactionDate >= rangeStart)
                        .Sum(t => (int?)(t.QuantityIn - t.QuantityOut)) ?? 0))
                    : query.OrderBy(s => s.CurrentStock - (_dbContext.InventoryTransactions
                        .Where(t => t.BranchId == s.BranchId
                                 && t.ProductVariantId == s.ProductVariantId
                                 && t.TransactionDate >= rangeStart)
                        .Sum(t => (int?)(t.QuantityIn - t.QuantityOut)) ?? 0)),

                "closing" => descending
                    ? query.OrderByDescending(s => s.CurrentStock - (_dbContext.InventoryTransactions
                        .Where(t => t.BranchId == s.BranchId
                                 && t.ProductVariantId == s.ProductVariantId
                                 && t.TransactionDate >= rangeEnd)
                        .Sum(t => (int?)(t.QuantityIn - t.QuantityOut)) ?? 0))
                    : query.OrderBy(s => s.CurrentStock - (_dbContext.InventoryTransactions
                        .Where(t => t.BranchId == s.BranchId
                                 && t.ProductVariantId == s.ProductVariantId
                                 && t.TransactionDate >= rangeEnd)
                        .Sum(t => (int?)(t.QuantityIn - t.QuantityOut)) ?? 0)),

                _ => descending
                    ? query.OrderByDescending(s => s.ProductVariant.Product.ProductName)
                    : query.OrderBy(s => s.ProductVariant.Product.ProductName),
            };

            return ordered.ThenBy(s => s.Id);
        }
    }
}
