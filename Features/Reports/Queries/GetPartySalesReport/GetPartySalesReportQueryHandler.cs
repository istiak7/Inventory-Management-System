using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Reports.Queries.GetPartySalesReport
{
    public class GetPartySalesReportQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetPartySalesReportQueryHandler> _logger
    ) : IRequestHandler<GetPartySalesReportQuery, Result>
    {
        public async Task<Result> Handle(GetPartySalesReportQuery request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.SaleType) &&
                !Enum.TryParse<SaleType>(request.SaleType, true, out _))
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"SaleType must be one of: {SaleType.Cash}, {SaleType.Debit}."
                };

            try
            {
                var (pageNumber, pageSize) = ReportPaging.Normalize(request.PageNumber, request.PageSize);
                var query = BuildQuery(request);

                var totalCount = await query.LongCountAsync(cancellationToken);

                var totalSubTotal = await query.SumAsync(s => (decimal?)s.SubTotal, cancellationToken) ?? 0m;
                var totalDiscount = await query.SumAsync(s => (decimal?)s.DiscountAmount, cancellationToken) ?? 0m;
                var totalTax = await query.SumAsync(s => (decimal?)s.TaxAmount, cancellationToken) ?? 0m;
                var totalSales = await query.SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0m;
                var totalPaid = await query.SumAsync(s => (decimal?)s.PaidAmount, cancellationToken) ?? 0m;
                var totalDue = await query.SumAsync(s => (decimal?)s.DueAmount, cancellationToken) ?? 0m;

                var totalAdjustment = await _dbContext.WarrantyClaims
                    .AsNoTracking()
                    .Where(w => (w.Resolution == WarrantyResolutionType.Replaced
                              || w.Resolution == WarrantyResolutionType.NotRepairable)
                             && query.Select(s => s.Id).Contains(w.SaleDetails.SaleId))
                    .SumAsync(w => (decimal?)w.SaleDetails.TotalAmount, cancellationToken) ?? 0m;

                var rawRows = await ApplySort(query, request)
                    .Skip(ReportPaging.SkipCount(pageNumber, pageSize))
                    .Take(pageSize)
                    .Select(s => new
                    {
                        s.Id,
                        s.InvoiceNumber,
                        s.SaleDate,
                        s.CustomerId,
                        CustomerName = s.Customer.Name,
                        s.Customer.PhoneNumber,
                        s.BranchId,
                        BranchName = s.Branch.Name,
                        s.SaleType,
                        s.Status,
                        s.SubTotal,
                        s.DiscountAmount,
                        s.TaxAmount,
                        s.TotalAmount,
                        s.PaidAmount,
                        s.DueAmount,
                        s.Remarks,
                        AdjustmentCount = _dbContext.WarrantyClaims
                            .Count(w => w.SaleDetails.SaleId == s.Id
                                     && (w.Resolution == WarrantyResolutionType.Replaced
                                      || w.Resolution == WarrantyResolutionType.NotRepairable)),
                        AdjustmentAmount = _dbContext.WarrantyClaims
                            .Where(w => w.SaleDetails.SaleId == s.Id
                                     && (w.Resolution == WarrantyResolutionType.Replaced
                                      || w.Resolution == WarrantyResolutionType.NotRepairable))
                            .Sum(w => (decimal?)w.SaleDetails.TotalAmount) ?? 0m,
                        Payments = s.SaleCustomerPayments
                            .OrderBy(sp => sp.Id)
                            .Select(sp => new
                            {
                                sp.CustomerPaymentId,
                                sp.Amount,
                                sp.CustomerPayment.PaymentDate,
                                sp.CustomerPayment.PaymentMethod
                            })
                            .ToList()
                    })
                    .ToListAsync(cancellationToken);

                var rows = rawRows
                    .Select(s => new PartySalesRow(
                        s.Id,
                        s.InvoiceNumber,
                        s.SaleDate,
                        s.CustomerId,
                        s.CustomerName,
                        s.PhoneNumber,
                        s.BranchId,
                        s.BranchName,
                        s.SaleType.ToString(),
                        s.Status.ToString(),
                        s.SubTotal,
                        s.DiscountAmount,
                        s.TaxAmount,
                        s.TotalAmount,
                        s.PaidAmount,
                        s.DueAmount,
                        s.AdjustmentCount,
                        s.AdjustmentAmount,
                        s.Remarks,
                        s.Payments
                            .Select(p => new ReportPaymentAllocation(
                                p.CustomerPaymentId, p.Amount, p.PaymentDate, p.PaymentMethod))
                            .ToList()))
                    .ToList();

                var response = new PartySalesReportResponse(
                    new PartySalesSummary(
                        totalCount,
                        totalSubTotal,
                        totalDiscount,
                        totalTax,
                        totalSales,
                        totalPaid,
                        totalDue,
                        totalAdjustment),
                    ReportPaging.Page(rows, totalCount, pageNumber, pageSize));

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Party sales report retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving the party sales report");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the party sales report."
                };
            }
        }

        private IQueryable<CustomerSale> BuildQuery(GetPartySalesReportQuery request)
        {
            var query = _dbContext.CustomerSales
                .AsNoTracking()
                .Where(s => s.IsActive != (int)EntityStatus.Deleted);

            var range = ReportDateRange.From(request.StartDate, request.EndDate);

            if (range.Start is DateTime start)
                query = query.Where(s => s.SaleDate >= start);

            if (range.EndExclusive is DateTime end)
                query = query.Where(s => s.SaleDate < end);

            if (request.CustomerId is int customerId)
                query = query.Where(s => s.CustomerId == customerId);

            if (request.BranchId is int branchId)
                query = query.Where(s => s.BranchId == branchId);

            if (!string.IsNullOrWhiteSpace(request.SaleType) &&
                Enum.TryParse<SaleType>(request.SaleType, true, out var saleType))
                query = query.Where(s => s.SaleType == saleType);

            if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
            {
                var invoiceTerm = $"%{request.InvoiceNumber.Trim()}%";
                query = query.Where(s => EF.Functions.ILike(s.InvoiceNumber, invoiceTerm));
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = $"%{request.Search.Trim()}%";
                var phone = CustomerPhoneNumber.Normalize(request.Search);
                var phoneTerm = phone.Length > 0 ? $"%{phone}%" : null;

                query = query.Where(s =>
                    EF.Functions.ILike(s.InvoiceNumber, term) ||
                    EF.Functions.ILike(s.Customer.Name, term) ||
                    (phoneTerm != null && EF.Functions.ILike(s.Customer.PhoneNumber, phoneTerm)));
            }

            return query;
        }

        private static IQueryable<CustomerSale> ApplySort(IQueryable<CustomerSale> query, GetPartySalesReportQuery request)
        {
            var descending = request.SortDescending;

            IOrderedQueryable<CustomerSale> ordered = request.SortBy?.Trim().ToLowerInvariant() switch
            {
                "invoice" => descending
                    ? query.OrderByDescending(s => s.InvoiceNumber)
                    : query.OrderBy(s => s.InvoiceNumber),

                "party" => descending
                    ? query.OrderByDescending(s => s.Customer.Name)
                    : query.OrderBy(s => s.Customer.Name),

                "total" => descending
                    ? query.OrderByDescending(s => s.TotalAmount)
                    : query.OrderBy(s => s.TotalAmount),

                "paid" => descending
                    ? query.OrderByDescending(s => s.PaidAmount)
                    : query.OrderBy(s => s.PaidAmount),

                "due" => descending
                    ? query.OrderByDescending(s => s.DueAmount)
                    : query.OrderBy(s => s.DueAmount),

                _ => descending
                    ? query.OrderByDescending(s => s.SaleDate)
                    : query.OrderBy(s => s.SaleDate),
            };

            return descending ? ordered.ThenByDescending(s => s.Id) : ordered.ThenBy(s => s.Id);
        }
    }
}
