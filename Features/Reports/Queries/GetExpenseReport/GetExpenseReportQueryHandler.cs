using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Reports.Queries.GetExpenseReport
{
    public class GetExpenseReportQueryHandler(
        AppDbContext _dbContext,
        ICurrentUser _currentUser,
        ILogger<GetExpenseReportQueryHandler> _logger
    ) : IRequestHandler<GetExpenseReportQuery, Result>
    {
        public async Task<Result> Handle(GetExpenseReportQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var (pageNumber, pageSize) = ReportPaging.Normalize(request.PageNumber, request.PageSize, request.IsExport);
                var skip = ReportPaging.SkipCount(pageNumber, pageSize);

                var query = BuildQuery(request);

                var totalCount = await query.LongCountAsync(cancellationToken);
                var totalAmount = await query.SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

                var byCategory = await query
                    .GroupBy(e => e.ExpenseCategory!.Name)
                    .Select(g => new { Name = g.Key, Amount = g.Sum(e => e.Amount) })
                    .OrderByDescending(g => g.Amount)
                    .ToListAsync(cancellationToken);

                var top = byCategory.FirstOrDefault();

                var raw = await Sort(query, request.SortBy?.Trim().ToLowerInvariant(), request.SortDescending)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(e => new
                    {
                        e.Id,
                        e.ExpenseDate,
                        e.Name,
                        e.Description,
                        e.ExpenseCategoryId,
                        CategoryName = e.ExpenseCategory!.Name,
                        e.BranchId,
                        BranchName = e.Branch!.Name,
                        e.PaymentMethod,
                        RecordedBy = e.RecordByUser != null ? e.RecordByUser.Name : null,
                        e.Amount
                    })
                    .ToListAsync(cancellationToken);

                var rows = raw
                    .Select(e => new ExpenseReportRow(
                        e.Id,
                        "EXP-" + e.Id,
                        e.ExpenseDate,
                        e.Name,
                        e.Description,
                        e.ExpenseCategoryId,
                        e.CategoryName,
                        e.BranchId,
                        e.BranchName,
                        e.PaymentMethod,
                        e.RecordedBy,
                        e.Amount))
                    .ToList();

                var summary = new ExpenseReportSummary(
                    totalCount,
                    totalAmount,
                    totalCount == 0 ? 0m : Math.Round(totalAmount / totalCount, 2),
                    byCategory.Count,
                    top?.Name,
                    top?.Amount ?? 0m);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Expense report retrieved successfully",
                    Data = new ExpenseReportResponse(summary, ReportPaging.Page(rows, totalCount, pageNumber, pageSize))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving the expense report");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the expense report."
                };
            }
        }

        private IQueryable<Expense> BuildQuery(GetExpenseReportQuery request)
        {
            var query = _dbContext.Expenses.AsNoTracking();

            // Expenses carry no branch query filter in AppDbContext, unlike sales and payments, so
            // a user tied to one branch is scoped here by hand.
            if (_currentUser.BranchId is int userBranchId)
                query = query.Where(e => e.BranchId == userBranchId);

            var range = ReportDateRange.From(request.StartDate, request.EndDate);

            if (range.Start is DateTime start)
                query = query.Where(e => e.ExpenseDate >= start);

            if (range.EndExclusive is DateTime end)
                query = query.Where(e => e.ExpenseDate < end);

            if (request.CategoryId is int categoryId && categoryId > 0)
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
                    EF.Functions.ILike(e.Name!, term) ||
                    EF.Functions.ILike(e.Description!, term) ||
                    EF.Functions.ILike(e.ExpenseCategory!.Name, term));
            }

            return query;
        }

        private static IQueryable<Expense> Sort(IQueryable<Expense> query, string? sortBy, bool descending)
        {
            IOrderedQueryable<Expense> ordered = sortBy switch
            {
                "category" => descending
                    ? query.OrderByDescending(e => e.ExpenseCategory!.Name)
                    : query.OrderBy(e => e.ExpenseCategory!.Name),

                "name" => descending
                    ? query.OrderByDescending(e => e.Name)
                    : query.OrderBy(e => e.Name),

                "amount" => descending
                    ? query.OrderByDescending(e => e.Amount)
                    : query.OrderBy(e => e.Amount),

                _ => descending
                    ? query.OrderByDescending(e => e.ExpenseDate)
                    : query.OrderBy(e => e.ExpenseDate),
            };

            return descending ? ordered.ThenByDescending(e => e.Id) : ordered.ThenBy(e => e.Id);
        }
    }
}
