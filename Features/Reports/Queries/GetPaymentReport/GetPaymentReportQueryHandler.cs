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
        ICurrentUser _currentUser,
        ILogger<GetPaymentReportQueryHandler> _logger
    ) : IRequestHandler<GetPaymentReportQuery, Result>
    {
        public async Task<Result> Handle(GetPaymentReportQuery request, CancellationToken cancellationToken)
        {
            if (!ReportPartyTypes.IsValid(request.PartyType) && !ReportPartyTypes.IsExpense(request.PartyType))
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"PartyType must be one of: {ReportPartyTypes.All}, {ReportPartyTypes.Customer}, {ReportPartyTypes.Supplier}, {ReportPartyTypes.Expense}."
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
                var (pageNumber, pageSize) = ReportPaging.Normalize(request.PageNumber, request.PageSize, request.IsExport);
                var skip = ReportPaging.SkipCount(pageNumber, pageSize);
                var sortBy = request.SortBy?.Trim().ToLowerInvariant();

                var includeCustomer = ReportPartyTypes.IncludesCustomer(request.PartyType)
                                   && ReportPaymentDirections.IncludesReceived(request.Direction);

                var includeSupplier = ReportPartyTypes.IncludesSupplier(request.PartyType)
                                   && ReportPaymentDirections.IncludesPaid(request.Direction);

                // An office expense is money out with no invoice behind it, so it only belongs in
                // the "Paid" view, has no allocation status to filter on, and its party is its
                // category: a customer or supplier id cannot select one.
                var includeExpense = ReportPartyTypes.IncludesExpense(request.PartyType)
                                  && ReportPaymentDirections.IncludesPaid(request.Direction)
                                  && string.IsNullOrWhiteSpace(request.Status)
                                  && (request.PartyId is null || ReportPartyTypes.IsExpense(request.PartyType));

                var customerQuery = includeCustomer ? BuildCustomerQuery(request) : null;
                var supplierQuery = includeSupplier ? BuildSupplierQuery(request) : null;
                var expenseQuery = includeExpense ? BuildExpenseQuery(request) : null;

                var customerCount = customerQuery is null ? 0L : await customerQuery.LongCountAsync(cancellationToken);
                var supplierCount = supplierQuery is null ? 0L : await supplierQuery.LongCountAsync(cancellationToken);
                var expenseCount = expenseQuery is null ? 0L : await expenseQuery.LongCountAsync(cancellationToken);
                var totalCount = customerCount + supplierCount + expenseCount;

                var totalReceived = customerQuery is null
                    ? 0m
                    : await customerQuery.SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

                var totalSupplierPaid = supplierQuery is null
                    ? 0m
                    : await supplierQuery.SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

                var totalExpenses = expenseQuery is null
                    ? 0m
                    : await expenseQuery.SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

                // Paid is all money out: supplier payments and office expenses alike.
                var totalPaid = totalSupplierPaid + totalExpenses;

                var sourceCount = (customerQuery is null ? 0 : 1)
                                + (supplierQuery is null ? 0 : 1)
                                + (expenseQuery is null ? 0 : 1);
                var merging = sourceCount > 1;
                var take = merging ? ReportPaging.TakeForMerge(pageNumber, pageSize) : pageSize;
                var sourceSkip = merging ? 0 : skip;

                var customerRows = customerQuery is null
                    ? new List<PaymentReportRow>()
                    : await LoadCustomerRowsAsync(customerQuery, sortBy, request.SortDescending, sourceSkip, take, cancellationToken);

                var supplierRows = supplierQuery is null
                    ? new List<PaymentReportRow>()
                    : await LoadSupplierRowsAsync(supplierQuery, sortBy, request.SortDescending, sourceSkip, take, cancellationToken);

                var expenseRows = expenseQuery is null
                    ? new List<PaymentReportRow>()
                    : await LoadExpenseRowsAsync(expenseQuery, sortBy, request.SortDescending, sourceSkip, take, cancellationToken);

                PagedResult<PaymentReportRow> page;

                if (merging)
                {
                    // MergePage concatenates before it sorts, so two of the sources can travel as one.
                    var outgoing = supplierRows.Concat(expenseRows).ToList();

                    page = sortBy switch
                    {
                        "party" => ReportPaging.MergePage(customerRows, outgoing,
                            r => (r.PartyName, r.PaymentDate, r.PaymentId), request.SortDescending, totalCount, pageNumber, pageSize),

                        "amount" => ReportPaging.MergePage(customerRows, outgoing,
                            r => (r.Amount, r.PaymentDate, r.PaymentId), request.SortDescending, totalCount, pageNumber, pageSize),

                        "method" => ReportPaging.MergePage(customerRows, outgoing,
                            r => (r.PaymentMethod, r.PaymentDate, r.PaymentId), request.SortDescending, totalCount, pageNumber, pageSize),

                        _ => ReportPaging.MergePage(customerRows, outgoing,
                            r => (r.PaymentDate, r.PartyType, r.PaymentId), request.SortDescending, totalCount, pageNumber, pageSize),
                    };
                }
                else
                {
                    var items = customerQuery is not null ? customerRows
                              : supplierQuery is not null ? supplierRows
                              : expenseRows;
                    page = ReportPaging.Page(items, totalCount, pageNumber, pageSize);
                }

                var response = new PaymentReportResponse(
                    new PaymentReportSummary(totalCount, totalReceived, totalPaid, totalReceived - totalPaid, totalExpenses),
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

        private async Task<List<PaymentReportRow>> LoadExpenseRowsAsync(
            IQueryable<Expense> query,
            string? sortBy,
            bool descending,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var raw = await SortExpenses(query, sortBy, descending)
                .Skip(skip)
                .Take(take)
                .Select(e => new
                {
                    e.Id,
                    e.ExpenseCategoryId,
                    CategoryName = e.ExpenseCategory!.Name,
                    e.ExpenseDate,
                    e.PaymentMethod,
                    e.BranchId,
                    BranchName = e.Branch!.Name,
                    e.Amount,
                    e.Name,
                    e.Description
                })
                .ToListAsync(cancellationToken);

            return raw
                .Select(e => new PaymentReportRow(
                    e.Id,
                    ReportPartyTypes.Expense,
                    e.ExpenseCategoryId,
                    e.CategoryName,
                    ReportPaymentDirections.Paid,
                    e.ExpenseDate,
                    e.PaymentMethod,
                    e.BranchId,
                    e.BranchName,
                    "EXP-" + e.Id,
                    e.Amount,
                    // Nothing to allocate: an expense is spent in full the moment it is recorded.
                    e.Amount,
                    0m,
                    ReportPaymentStatuses.Expense,
                    ExpenseRemark(e.Name, e.Description),
                    Array.Empty<string>()))
                .ToList();
        }

        private static string? ExpenseRemark(string? name, string? description)
        {
            var parts = new[] { name?.Trim(), description?.Trim() }
                .Where(part => !string.IsNullOrEmpty(part))
                .ToList();
            return parts.Count == 0 ? null : string.Join(" — ", parts);
        }

        private IQueryable<Expense> BuildExpenseQuery(GetPaymentReportQuery request)
        {
            var query = _dbContext.Expenses.AsNoTracking();

            // Expenses carry no branch query filter in AppDbContext, unlike the payment tables, so
            // a user tied to one branch is scoped here by hand.
            if (_currentUser.BranchId is int userBranchId)
                query = query.Where(e => e.BranchId == userBranchId);

            var range = ReportDateRange.From(request.StartDate, request.EndDate);

            if (range.Start is DateTime start)
                query = query.Where(e => e.ExpenseDate >= start);

            if (range.EndExclusive is DateTime end)
                query = query.Where(e => e.ExpenseDate < end);

            // With the Expense party type selected, the party is an expense category.
            if (request.PartyId is int categoryId && categoryId > 0)
                query = query.Where(e => e.ExpenseCategoryId == categoryId);

            if (request.BranchId is int branchId)
                query = query.Where(e => e.BranchId == branchId);

            if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                var method = request.PaymentMethod.Trim();
                query = query.Where(e => e.PaymentMethod == method);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = $"%{request.Search.Trim()}%";
                query = query.Where(e =>
                    EF.Functions.ILike(e.ExpenseCategory!.Name, term) ||
                    EF.Functions.ILike(e.Name!, term) ||
                    EF.Functions.ILike(e.Description!, term));
            }

            return query;
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

        private static IQueryable<Expense> SortExpenses(IQueryable<Expense> query, string? sortBy, bool descending)
        {
            IOrderedQueryable<Expense> ordered = sortBy switch
            {
                "party" => descending
                    ? query.OrderByDescending(e => e.ExpenseCategory!.Name)
                    : query.OrderBy(e => e.ExpenseCategory!.Name),

                "amount" => descending
                    ? query.OrderByDescending(e => e.Amount)
                    : query.OrderBy(e => e.Amount),

                "method" => descending
                    ? query.OrderByDescending(e => e.PaymentMethod)
                    : query.OrderBy(e => e.PaymentMethod),

                _ => descending
                    ? query.OrderByDescending(e => e.ExpenseDate)
                    : query.OrderBy(e => e.ExpenseDate),
            };

            return descending ? ordered.ThenByDescending(e => e.Id) : ordered.ThenBy(e => e.Id);
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
