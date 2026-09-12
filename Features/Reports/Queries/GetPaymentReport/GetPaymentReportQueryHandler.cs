using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Reports.Queries.GetPaymentReport
{
    public class GetPaymentReportQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetPaymentReportQueryHandler> _logger
    ) : IRequestHandler<GetPaymentReportQuery, Result>
    {
        public async Task<Result> Handle(GetPaymentReportQuery request, CancellationToken cancellationToken)
        {
            if (!ReportPartyTypes.IsValid(request.PartyType))
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"PartyType must be one of: {ReportPartyTypes.All}, {ReportPartyTypes.Customer}, {ReportPartyTypes.Supplier}."
                };

            if (!ReportPaymentDirections.IsValid(request.Direction))
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"Direction must be one of: {ReportPaymentDirections.All}, {ReportPaymentDirections.Received}, {ReportPaymentDirections.Paid}."
                };

            try
            {
                var (pageNumber, pageSize) = ReportPaging.Normalize(request.PageNumber, request.PageSize);
                var skip = ReportPaging.SkipCount(pageNumber, pageSize);
                var sortBy = request.SortBy?.Trim().ToLowerInvariant();

                var includeCustomer = ReportPartyTypes.IncludesCustomer(request.PartyType)
                                   && ReportPaymentDirections.IncludesReceived(request.Direction);

                var includeSupplier = ReportPartyTypes.IncludesSupplier(request.PartyType)
                                   && ReportPaymentDirections.IncludesPaid(request.Direction);

                var customerQuery = includeCustomer ? BuildCustomerQuery(request) : null;
                var supplierQuery = includeSupplier ? BuildSupplierQuery(request) : null;

                var customerCount = customerQuery is null ? 0L : await customerQuery.LongCountAsync(cancellationToken);
                var supplierCount = supplierQuery is null ? 0L : await supplierQuery.LongCountAsync(cancellationToken);
                var totalCount = customerCount + supplierCount;

                var totalReceived = customerQuery is null
                    ? 0m
                    : await customerQuery.SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

                var totalPaid = supplierQuery is null
                    ? 0m
                    : await supplierQuery.SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

                var bothSides = customerQuery is not null && supplierQuery is not null;
                var take = bothSides ? ReportPaging.TakeForMerge(pageNumber, pageSize) : pageSize;
                var sourceSkip = bothSides ? 0 : skip;

                var customerRows = customerQuery is null
                    ? new List<PaymentReportRow>()
                    : await LoadCustomerRowsAsync(customerQuery, sortBy, request.SortDescending, sourceSkip, take, cancellationToken);

                var supplierRows = supplierQuery is null
                    ? new List<PaymentReportRow>()
                    : await LoadSupplierRowsAsync(supplierQuery, sortBy, request.SortDescending, sourceSkip, take, cancellationToken);

                PagedResult<PaymentReportRow> page;

                if (bothSides)
                {
                    page = sortBy switch
                    {
                        "party" => ReportPaging.MergePage(customerRows, supplierRows,
                            r => (r.PartyName, r.PaymentDate, r.PaymentId), request.SortDescending, totalCount, pageNumber, pageSize),

                        "amount" => ReportPaging.MergePage(customerRows, supplierRows,
                            r => (r.Amount, r.PaymentDate, r.PaymentId), request.SortDescending, totalCount, pageNumber, pageSize),

                        "method" => ReportPaging.MergePage(customerRows, supplierRows,
                            r => (r.PaymentMethod, r.PaymentDate, r.PaymentId), request.SortDescending, totalCount, pageNumber, pageSize),

                        _ => ReportPaging.MergePage(customerRows, supplierRows,
                            r => (r.PaymentDate, r.PartyType, r.PaymentId), request.SortDescending, totalCount, pageNumber, pageSize),
                    };
                }
                else
                {
                    var items = customerQuery is not null ? customerRows : supplierRows;
                    page = ReportPaging.Page(items, totalCount, pageNumber, pageSize);
                }

                var response = new PaymentReportResponse(
                    new PaymentReportSummary(totalCount, totalReceived, totalPaid, totalReceived - totalPaid),
                    page);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Payment report retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving the payment report");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the payment report."
                };
            }
        }

        private async Task<List<PaymentReportRow>> LoadCustomerRowsAsync(
            IQueryable<CustomerPayment> query,
            string? sortBy,
            bool descending,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var raw = await SortCustomers(query, sortBy, descending)
                .Skip(skip)
                .Take(take)
                .Select(p => new
                {
                    p.Id,
                    PartyId = p.CustomerId,
                    PartyName = p.Customer.Name,
                    p.PaymentDate,
                    p.PaymentMethod,
                    p.BranchId,
                    BranchName = p.Branch.Name,
                    p.Amount,
                    p.Remarks,
                    Allocated = p.SaleCustomerPayments.Sum(sp => (decimal?)sp.Amount) ?? 0m,
                    Invoices = p.SaleCustomerPayments
                        .OrderBy(sp => sp.Id)
                        .Select(sp => sp.CustomerSale.InvoiceNumber)
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            return raw
                .Select(p => new PaymentReportRow(
                    p.Id,
                    ReportPartyTypes.Customer,
                    p.PartyId,
                    p.PartyName,
                    ReportPaymentDirections.Received,
                    p.PaymentDate,
                    p.PaymentMethod,
                    p.BranchId,
                    p.BranchName,
                    "RCV-" + p.Id,
                    p.Amount,
                    p.Allocated,
                    p.Amount - p.Allocated,
                    ReportPaymentStatuses.For(p.Amount, p.Allocated),
                    p.Remarks,
                    p.Invoices))
                .ToList();
        }

        private async Task<List<PaymentReportRow>> LoadSupplierRowsAsync(
            IQueryable<SupplierPayment> query,
            string? sortBy,
            bool descending,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var raw = await SortSuppliers(query, sortBy, descending)
                .Skip(skip)
                .Take(take)
                .Select(p => new
                {
                    p.Id,
                    PartyId = p.SupplierId,
                    PartyName = p.Supplier.Name,
                    p.PaymentDate,
                    p.PaymentMethod,
                    p.BranchId,
                    BranchName = p.Branch.Name,
                    p.Amount,
                    p.Remarks,
                    Allocated = p.SupplierPurchasePayments.Sum(pp => (decimal?)pp.Amount) ?? 0m,
                    Invoices = p.SupplierPurchasePayments
                        .OrderBy(pp => pp.Id)
                        .Select(pp => pp.SupplierPurchase.InvoiceNumber)
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            return raw
                .Select(p => new PaymentReportRow(
                    p.Id,
                    ReportPartyTypes.Supplier,
                    p.PartyId,
                    p.PartyName,
                    ReportPaymentDirections.Paid,
                    p.PaymentDate,
                    p.PaymentMethod,
                    p.BranchId,
                    p.BranchName,
                    "PAY-" + p.Id,
                    p.Amount,
                    p.Allocated,
                    p.Amount - p.Allocated,
                    ReportPaymentStatuses.For(p.Amount, p.Allocated),
                    p.Remarks,
                    p.Invoices))
                .ToList();
        }

        private IQueryable<CustomerPayment> BuildCustomerQuery(GetPaymentReportQuery request)
        {
            var query = _dbContext.CustomerPayments.AsNoTracking();

            var range = ReportDateRange.From(request.StartDate, request.EndDate);

            if (range.Start is DateTime start)
                query = query.Where(p => p.PaymentDate >= start);

            if (range.EndExclusive is DateTime end)
                query = query.Where(p => p.PaymentDate < end);

            if (request.PartyId is int partyId && partyId > 0)
                query = query.Where(p => p.CustomerId == partyId);

            if (request.BranchId is int branchId)
                query = query.Where(p => p.BranchId == branchId);

            if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                var method = request.PaymentMethod.Trim();
                query = query.Where(p => p.PaymentMethod == method);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = $"%{request.Search.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.Customer.Name, term) ||
                    p.SaleCustomerPayments.Any(sp => EF.Functions.ILike(sp.CustomerSale.InvoiceNumber, term)));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.Trim();

                if (status.Equals(ReportPaymentStatuses.Unallocated, StringComparison.OrdinalIgnoreCase))
                    query = query.Where(p => (p.SaleCustomerPayments.Sum(sp => (decimal?)sp.Amount) ?? 0m) <= 0m);
                else if (status.Equals(ReportPaymentStatuses.Allocated, StringComparison.OrdinalIgnoreCase))
                    query = query.Where(p => (p.SaleCustomerPayments.Sum(sp => (decimal?)sp.Amount) ?? 0m) >= p.Amount);
                else if (status.Equals(ReportPaymentStatuses.PartiallyAllocated, StringComparison.OrdinalIgnoreCase))
                    query = query.Where(p => (p.SaleCustomerPayments.Sum(sp => (decimal?)sp.Amount) ?? 0m) > 0m
                                          && (p.SaleCustomerPayments.Sum(sp => (decimal?)sp.Amount) ?? 0m) < p.Amount);
            }

            return query;
        }

        private IQueryable<SupplierPayment> BuildSupplierQuery(GetPaymentReportQuery request)
        {
            var query = _dbContext.SupplierPayments.AsNoTracking();

            var range = ReportDateRange.From(request.StartDate, request.EndDate);

            if (range.Start is DateTime start)
                query = query.Where(p => p.PaymentDate >= start);

            if (range.EndExclusive is DateTime end)
                query = query.Where(p => p.PaymentDate < end);

            if (request.PartyId is int partyId && partyId > 0)
                query = query.Where(p => p.SupplierId == partyId);

            if (request.BranchId is int branchId)
                query = query.Where(p => p.BranchId == branchId);

            if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                var method = request.PaymentMethod.Trim();
                query = query.Where(p => p.PaymentMethod == method);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = $"%{request.Search.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.Supplier.Name, term) ||
                    p.SupplierPurchasePayments.Any(pp => EF.Functions.ILike(pp.SupplierPurchase.InvoiceNumber, term)));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.Trim();

                if (status.Equals(ReportPaymentStatuses.Unallocated, StringComparison.OrdinalIgnoreCase))
                    query = query.Where(p => (p.SupplierPurchasePayments.Sum(pp => (decimal?)pp.Amount) ?? 0m) <= 0m);
                else if (status.Equals(ReportPaymentStatuses.Allocated, StringComparison.OrdinalIgnoreCase))
                    query = query.Where(p => (p.SupplierPurchasePayments.Sum(pp => (decimal?)pp.Amount) ?? 0m) >= p.Amount);
                else if (status.Equals(ReportPaymentStatuses.PartiallyAllocated, StringComparison.OrdinalIgnoreCase))
                    query = query.Where(p => (p.SupplierPurchasePayments.Sum(pp => (decimal?)pp.Amount) ?? 0m) > 0m
                                          && (p.SupplierPurchasePayments.Sum(pp => (decimal?)pp.Amount) ?? 0m) < p.Amount);
            }

            return query;
        }

        private static IQueryable<CustomerPayment> SortCustomers(IQueryable<CustomerPayment> query, string? sortBy, bool descending)
        {
            IOrderedQueryable<CustomerPayment> ordered = sortBy switch
            {
                "party" => descending
                    ? query.OrderByDescending(p => p.Customer.Name)
                    : query.OrderBy(p => p.Customer.Name),

                "amount" => descending
                    ? query.OrderByDescending(p => p.Amount)
                    : query.OrderBy(p => p.Amount),

                "method" => descending
                    ? query.OrderByDescending(p => p.PaymentMethod)
                    : query.OrderBy(p => p.PaymentMethod),

                _ => descending
                    ? query.OrderByDescending(p => p.PaymentDate)
                    : query.OrderBy(p => p.PaymentDate),
            };

            return descending ? ordered.ThenByDescending(p => p.Id) : ordered.ThenBy(p => p.Id);
        }

        private static IQueryable<SupplierPayment> SortSuppliers(IQueryable<SupplierPayment> query, string? sortBy, bool descending)
        {
            IOrderedQueryable<SupplierPayment> ordered = sortBy switch
            {
                "party" => descending
                    ? query.OrderByDescending(p => p.Supplier.Name)
                    : query.OrderBy(p => p.Supplier.Name),

                "amount" => descending
                    ? query.OrderByDescending(p => p.Amount)
                    : query.OrderBy(p => p.Amount),

                "method" => descending
                    ? query.OrderByDescending(p => p.PaymentMethod)
                    : query.OrderBy(p => p.PaymentMethod),

                _ => descending
                    ? query.OrderByDescending(p => p.PaymentDate)
                    : query.OrderBy(p => p.PaymentDate),
            };

            return descending ? ordered.ThenByDescending(p => p.Id) : ordered.ThenBy(p => p.Id);
        }
    }
}
