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

namespace Inventory_Management_System.Features.Reports.Queries.GetPartyPurchaseReport
{
    public class GetPartyPurchaseReportQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetPartyPurchaseReportQueryHandler> _logger
    ) : IRequestHandler<GetPartyPurchaseReportQuery, Result>
    {
        public async Task<Result> Handle(GetPartyPurchaseReportQuery request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.PurchaseType) &&
                !Enum.TryParse<PurchaseType>(request.PurchaseType, true, out _))
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"PurchaseType must be one of: {PurchaseType.Cash}, {PurchaseType.Debit}."
                };

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                !Enum.TryParse<PurchaseStatus>(request.Status, true, out _))
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"Status must be one of: {string.Join(", ", Enum.GetNames<PurchaseStatus>())}."
                };

            try
            {
                var (pageNumber, pageSize) = ReportPaging.Normalize(request.PageNumber, request.PageSize, request.IsExport);
                var query = BuildQuery(request);

                var totalCount = await query.LongCountAsync(cancellationToken);

                var totalPurchase = await query.SumAsync(p => (decimal?)p.TotalAmount, cancellationToken) ?? 0m;
                var totalPaid = await query.SumAsync(p => (decimal?)p.PaidAmount, cancellationToken) ?? 0m;
                var totalDue = await query.SumAsync(p => (decimal?)p.DueAmount, cancellationToken) ?? 0m;

                var totalAdjustment = await _dbContext.SupplierPurchaseDetails
                    .AsNoTracking()
                    .Where(d => d.IsActive != (int)EntityStatus.Deleted
                             && query.Select(p => p.Id).Contains(d.PurchaseId))
                    .SumAsync(d => (decimal?)((d.Status == LineStatus.Rejected
                        ? d.OrderedQuantity
                        : d.OrderedQuantity > (d.ReceivedQuantity ?? 0)
                            ? d.OrderedQuantity - (d.ReceivedQuantity ?? 0)
                            : 0) * d.UnitPrice), cancellationToken) ?? 0m;

                var rawRows = await ApplySort(query, request)
                    .Skip(ReportPaging.SkipCount(pageNumber, pageSize))
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.InvoiceNumber,
                        p.PurchaseDate,
                        p.SupplierId,
                        SupplierName = p.Supplier.Name,
                        p.Supplier.PhoneNumber,
                        p.BranchId,
                        BranchName = p.Branch.Name,
                        p.PurchaseType,
                        p.Status,
                        p.TotalAmount,
                        p.PaidAmount,
                        p.DueAmount,
                        p.Remarks,
                        AdjustmentQuantity = p.SupplierPurchaseDetails
                            .Where(d => d.IsActive != (int)EntityStatus.Deleted)
                            .Sum(d => (int?)(d.Status == LineStatus.Rejected
                                ? d.OrderedQuantity
                                : d.OrderedQuantity > (d.ReceivedQuantity ?? 0)
                                    ? d.OrderedQuantity - (d.ReceivedQuantity ?? 0)
                                    : 0)) ?? 0,
                        AdjustmentAmount = p.SupplierPurchaseDetails
                            .Where(d => d.IsActive != (int)EntityStatus.Deleted)
                            .Sum(d => (decimal?)((d.Status == LineStatus.Rejected
                                ? d.OrderedQuantity
                                : d.OrderedQuantity > (d.ReceivedQuantity ?? 0)
                                    ? d.OrderedQuantity - (d.ReceivedQuantity ?? 0)
                                    : 0) * d.UnitPrice)) ?? 0m,
                        Payments = p.SupplierPurchasePayments
                            .OrderBy(pp => pp.Id)
                            .Select(pp => new
                            {
                                pp.SupplierPaymentId,
                                pp.Amount,
                                pp.SupplierPayment.PaymentDate,
                                pp.SupplierPayment.PaymentMethod
                            })
                            .ToList()
                    })
                    .ToListAsync(cancellationToken);

                var rows = rawRows
                    .Select(p => new PartyPurchaseRow(
                        p.Id,
                        p.InvoiceNumber,
                        p.PurchaseDate,
                        p.SupplierId,
                        p.SupplierName,
                        p.PhoneNumber,
                        p.BranchId,
                        p.BranchName,
                        p.PurchaseType?.ToString(),
                        p.Status.ToString(),
                        p.TotalAmount,
                        p.PaidAmount,
                        p.DueAmount,
                        p.AdjustmentQuantity,
                        p.AdjustmentAmount,
                        p.Remarks,
                        p.Payments
                            .Select(x => new ReportPaymentAllocation(
                                x.SupplierPaymentId, x.Amount, x.PaymentDate, x.PaymentMethod))
                            .ToList()))
                    .ToList();

                var response = new PartyPurchaseReportResponse(
                    new PartyPurchaseSummary(totalCount, totalPurchase, totalPaid, totalDue, totalAdjustment),
                    ReportPaging.Page(rows, totalCount, pageNumber, pageSize));

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Party purchase report retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving the party purchase report");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the party purchase report."
                };
            }
        }

        private IQueryable<SupplierPurchase> BuildQuery(GetPartyPurchaseReportQuery request)
        {
            var query = _dbContext.SupplierPurchases
                .AsNoTracking()
                .Where(p => p.IsActive != (int)EntityStatus.Deleted);

            var range = ReportDateRange.From(request.StartDate, request.EndDate);

            if (range.Start is DateTime start)
                query = query.Where(p => p.PurchaseDate >= start);

            if (range.EndExclusive is DateTime end)
                query = query.Where(p => p.PurchaseDate < end);

            if (request.SupplierId is int supplierId)
                query = query.Where(p => p.SupplierId == supplierId);

            if (request.BranchId is int branchId)
                query = query.Where(p => p.BranchId == branchId);

            if (!string.IsNullOrWhiteSpace(request.PurchaseType) &&
                Enum.TryParse<PurchaseType>(request.PurchaseType, true, out var purchaseType))
                query = query.Where(p => p.PurchaseType == purchaseType);

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<PurchaseStatus>(request.Status, true, out var status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
            {
                var invoiceTerm = $"%{request.InvoiceNumber.Trim()}%";
                query = query.Where(p => EF.Functions.ILike(p.InvoiceNumber, invoiceTerm));
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = $"%{request.Search.Trim()}%";
                var phone = CustomerPhoneNumber.Normalize(request.Search);
                var phoneTerm = phone.Length > 0 ? $"%{phone}%" : null;

                query = query.Where(p =>
                    EF.Functions.ILike(p.InvoiceNumber, term) ||
                    EF.Functions.ILike(p.Supplier.Name, term) ||
                    (phoneTerm != null && EF.Functions.ILike(p.Supplier.PhoneNumber, phoneTerm)));
            }

            return query;
        }

        private static IQueryable<SupplierPurchase> ApplySort(IQueryable<SupplierPurchase> query, GetPartyPurchaseReportQuery request)
        {
            var descending = request.SortDescending;

            IOrderedQueryable<SupplierPurchase> ordered = request.SortBy?.Trim().ToLowerInvariant() switch
            {
                "invoice" => descending
                    ? query.OrderByDescending(p => p.InvoiceNumber)
                    : query.OrderBy(p => p.InvoiceNumber),

                "party" => descending
                    ? query.OrderByDescending(p => p.Supplier.Name)
                    : query.OrderBy(p => p.Supplier.Name),

                "total" => descending
                    ? query.OrderByDescending(p => p.TotalAmount)
                    : query.OrderBy(p => p.TotalAmount),

                "paid" => descending
                    ? query.OrderByDescending(p => p.PaidAmount)
                    : query.OrderBy(p => p.PaidAmount),

                "due" => descending
                    ? query.OrderByDescending(p => p.DueAmount)
                    : query.OrderBy(p => p.DueAmount),

                _ => descending
                    ? query.OrderByDescending(p => p.PurchaseDate)
                    : query.OrderBy(p => p.PurchaseDate),
            };

            return descending ? ordered.ThenByDescending(p => p.Id) : ordered.ThenBy(p => p.Id);
        }
    }
}
