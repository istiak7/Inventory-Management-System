using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Reports.Queries.GetLedgerReport
{
    public class GetLedgerReportQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetLedgerReportQueryHandler> _logger
    ) : IRequestHandler<GetLedgerReportQuery, Result>
    {
        public async Task<Result> Handle(GetLedgerReportQuery request, CancellationToken cancellationToken)
        {
            if (!ReportPartyTypes.IsValid(request.PartyType))
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"PartyType must be one of: {ReportPartyTypes.All}, {ReportPartyTypes.Customer}, {ReportPartyTypes.Supplier}."
                };

            try
            {
                var (pageNumber, pageSize) = ReportPaging.Normalize(request.PageNumber, request.PageSize, request.IsExport);
                var range = ReportDateRange.From(request.StartDate, request.EndDate);
                var skip = ReportPaging.SkipCount(pageNumber, pageSize);

                var includeCustomer = ReportPartyTypes.IncludesCustomer(request.PartyType);
                var includeSupplier = ReportPartyTypes.IncludesSupplier(request.PartyType);
                var singleParty = request.PartyId is > 0 && includeCustomer != includeSupplier;

                var customerAll = includeCustomer ? BuildCustomerQuery(request) : null;
                var supplierAll = includeSupplier ? BuildSupplierQuery(request) : null;

                var customerRange = customerAll is null ? null : ApplyRange(customerAll, range);
                var supplierRange = supplierAll is null ? null : ApplyRange(supplierAll, range);

                var customerCount = customerRange is null ? 0L : await customerRange.LongCountAsync(cancellationToken);
                var supplierCount = supplierRange is null ? 0L : await supplierRange.LongCountAsync(cancellationToken);
                var totalCount = customerCount + supplierCount;

                decimal totalDebit = 0m;
                decimal totalCredit = 0m;

                if (customerRange is not null)
                {
                    totalDebit += await customerRange.SumAsync(t => (decimal?)t.Debit, cancellationToken) ?? 0m;
                    totalCredit += await customerRange.SumAsync(t => (decimal?)t.Credit, cancellationToken) ?? 0m;
                }

                if (supplierRange is not null)
                {
                    totalDebit += await supplierRange.SumAsync(t => (decimal?)t.Debit, cancellationToken) ?? 0m;
                    totalCredit += await supplierRange.SumAsync(t => (decimal?)t.Credit, cancellationToken) ?? 0m;
                }

                decimal? openingBalance = null;
                if (singleParty)
                {
                    openingBalance = range.Start is DateTime start
                        ? includeCustomer
                            ? await customerAll!
                                .Where(t => t.TransactionDate < start)
                                .SumAsync(t => (decimal?)(t.Credit - t.Debit), cancellationToken) ?? 0m
                            : await supplierAll!
                                .Where(t => t.TransactionDate < start)
                                .SumAsync(t => (decimal?)(t.Debit - t.Credit), cancellationToken) ?? 0m
                        : 0m;
                }

                PagedResult<LedgerReportRow> page;

                if (customerRange is not null && supplierRange is not null)
                {
                    var take = ReportPaging.TakeForMerge(pageNumber, pageSize);

                    var customerRows = await OrderAndProject(customerRange).Take(take).ToListAsync(cancellationToken);
                    var supplierRows = await OrderAndProject(supplierRange).Take(take).ToListAsync(cancellationToken);

                    page = ReportPaging.MergePage(
                        customerRows,
                        supplierRows,
                        r => (r.TransactionDate, r.PartyType, r.Id),
                        false,
                        totalCount,
                        pageNumber,
                        pageSize);
                }
                else
                {
                    var items = customerRange is not null
                        ? await OrderAndProject(customerRange).Skip(skip).Take(pageSize).ToListAsync(cancellationToken)
                        : supplierRange is not null
                            ? await OrderAndProject(supplierRange).Skip(skip).Take(pageSize).ToListAsync(cancellationToken)
                            : new List<LedgerReportRow>();

                    page = ReportPaging.Page(items, totalCount, pageNumber, pageSize);
                }

                var carried = openingBalance ?? 0m;
                if (singleParty && skip > 0)
                {
                    carried += includeCustomer
                        ? await customerRange!
                            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Id)
                            .Take(skip)
                            .SumAsync(t => (decimal?)(t.Credit - t.Debit), cancellationToken) ?? 0m
                        : await supplierRange!
                            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Id)
                            .Take(skip)
                            .SumAsync(t => (decimal?)(t.Debit - t.Credit), cancellationToken) ?? 0m;
                }

                var rows = new List<LedgerReportRow>(page.Items.Count);
                var running = carried;

                foreach (var row in page.Items)
                {
                    if (singleParty)
                    {
                        running += SignedMovement(row);
                        rows.Add(row with { RunningBalance = running });
                    }
                    else
                    {
                        rows.Add(row with { RunningBalance = row.BalanceAfter });
                    }
                }

                decimal? closingBalance = singleParty
                    ? (openingBalance ?? 0m) + (includeCustomer ? totalCredit - totalDebit : totalDebit - totalCredit)
                    : null;

                var response = new LedgerReportResponse(
                    new LedgerReportSummary(openingBalance, totalDebit, totalCredit, closingBalance, totalCount),
                    ReportPaging.Page(rows, totalCount, pageNumber, pageSize));

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Ledger report retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving the ledger report");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the ledger report."
                };
            }
        }

        private static decimal SignedMovement(LedgerReportRow row) =>
            row.PartyType == ReportPartyTypes.Customer
                ? row.Credit - row.Debit
                : row.Debit - row.Credit;

        private IQueryable<CustomerTransaction> BuildCustomerQuery(GetLedgerReportQuery request)
        {
            var query = _dbContext.CustomerTransactions.AsNoTracking();

            if (request.PartyId is int partyId && partyId > 0)
                query = query.Where(t => t.CustomerId == partyId);

            if (request.BranchId is int branchId)
                query = query.Where(t =>
                    (t.CustomerSale != null && t.CustomerSale.BranchId == branchId) ||
                    (t.CustomerPayment != null && t.CustomerPayment.BranchId == branchId));

            if (!string.IsNullOrWhiteSpace(request.TransactionType))
            {
                var transactionType = request.TransactionType.Trim();
                query = query.Where(t => t.TransactionType == transactionType);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = $"%{request.Search.Trim()}%";
                query = query.Where(t =>
                    EF.Functions.ILike(t.Customer.Name, term) ||
                    (t.CustomerSale != null && EF.Functions.ILike(t.CustomerSale.InvoiceNumber, term)) ||
                    (t.CustomerPayment != null && t.CustomerPayment.SaleCustomerPayments.Any(sp =>
                        EF.Functions.ILike(sp.CustomerSale.InvoiceNumber, term))));
            }

            return query;
        }

        private IQueryable<SupplierTransaction> BuildSupplierQuery(GetLedgerReportQuery request)
        {
            var query = _dbContext.SupplierTransactions.AsNoTracking();

            if (request.PartyId is int partyId && partyId > 0)
                query = query.Where(t => t.SupplierId == partyId);

            if (request.BranchId is int branchId)
                query = query.Where(t =>
                    (t.SupplierPurchase != null && t.SupplierPurchase.BranchId == branchId) ||
                    (t.SupplierPayment != null && t.SupplierPayment.BranchId == branchId));

            if (!string.IsNullOrWhiteSpace(request.TransactionType))
            {
                var transactionType = request.TransactionType.Trim();
                query = query.Where(t => t.TransactionType == transactionType);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = $"%{request.Search.Trim()}%";
                query = query.Where(t =>
                    EF.Functions.ILike(t.Supplier.Name, term) ||
                    (t.SupplierPurchase != null && EF.Functions.ILike(t.SupplierPurchase.InvoiceNumber, term)) ||
                    (t.SupplierPayment != null && t.SupplierPayment.SupplierPurchasePayments.Any(pp =>
                        EF.Functions.ILike(pp.SupplierPurchase.InvoiceNumber, term))));
            }

            return query;
        }

        private static IQueryable<CustomerTransaction> ApplyRange(IQueryable<CustomerTransaction> query, ReportDateRange range)
        {
            if (range.Start is DateTime start)
                query = query.Where(t => t.TransactionDate >= start);

            if (range.EndExclusive is DateTime end)
                query = query.Where(t => t.TransactionDate < end);

            return query;
        }

        private static IQueryable<SupplierTransaction> ApplyRange(IQueryable<SupplierTransaction> query, ReportDateRange range)
        {
            if (range.Start is DateTime start)
                query = query.Where(t => t.TransactionDate >= start);

            if (range.EndExclusive is DateTime end)
                query = query.Where(t => t.TransactionDate < end);

            return query;
        }

        private static IQueryable<LedgerReportRow> OrderAndProject(IQueryable<CustomerTransaction> query) =>
            query
                .OrderBy(t => t.TransactionDate)
                .ThenBy(t => t.Id)
                .Select(t => new LedgerReportRow(
                    t.Id,
                    ReportPartyTypes.Customer,
                    t.CustomerId,
                    t.Customer.Name,
                    t.TransactionType,
                    t.TransactionDate,
                    t.SaleId != null
                        ? (t.CustomerSale!.InvoiceNumber ?? ("SAL-" + t.SaleId))
                        : t.CustomerPaymentId != null
                            ? ("PAY-" + t.CustomerPaymentId)
                            : ("TXN-" + t.Id),
                    t.SaleId != null
                        ? t.CustomerSale!.Remarks
                        : t.CustomerPaymentId != null
                            ? t.CustomerPayment!.Remarks
                            : null,
                    t.Debit,
                    t.Credit,
                    t.BalanceAfter,
                    0m));

        private static IQueryable<LedgerReportRow> OrderAndProject(IQueryable<SupplierTransaction> query) =>
            query
                .OrderBy(t => t.TransactionDate)
                .ThenBy(t => t.Id)
                .Select(t => new LedgerReportRow(
                    t.Id,
                    ReportPartyTypes.Supplier,
                    t.SupplierId,
                    t.Supplier.Name,
                    t.TransactionType,
                    t.TransactionDate,
                    t.SupplierPurchaseId != null
                        ? (t.SupplierPurchase!.InvoiceNumber ?? ("PUR-" + t.SupplierPurchaseId))
                        : t.SupplierPaymentId != null
                            ? ("PAY-" + t.SupplierPaymentId)
                            : ("TXN-" + t.Id),
                    t.SupplierPurchaseId != null
                        ? t.SupplierPurchase!.Remarks
                        : t.SupplierPaymentId != null
                            ? t.SupplierPayment!.Remarks
                            : null,
                    t.Debit,
                    t.Credit,
                    t.BalanceAfter,
                    0m));
    }
}
