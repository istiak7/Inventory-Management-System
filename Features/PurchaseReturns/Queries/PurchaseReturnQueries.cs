using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.PurchaseReturns.Shared;
using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Returns.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.PurchaseReturns.Queries
{
    // ---------------------------------------------------------------- requests
    public sealed record GetPurchaseReturnableQuery(int PurchaseId) : IRequest<Result>;

    public sealed record GetPurchaseReturnsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? Search = null,
        string? Status = null,
        int? BranchId = null,
        int? PurchaseId = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null
    ) : IRequest<Result>;

    public sealed record GetPurchaseReturnByIdQuery(int Id) : IRequest<Result>;

    // ---------------------------------------------------------------- responses
    public sealed record PurchaseReturnableResponse(
        int PurchaseId,
        string InvoiceNumber,
        DateTime PurchaseDate,
        string Status,
        string? PurchaseType,
        int SupplierId,
        string SupplierName,
        string SupplierPhoneNumber,
        int BranchId,
        string BranchName,
        decimal DiscountAmount,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueAmount,
        decimal ReturnedAmount,
        decimal SettledAmount,
        IReadOnlyList<PurchaseReturnableLine> Lines,
        IReadOnlyList<PurchaseReturnSummary> PreviousReturns
    );

    public sealed record PurchaseReturnableLine(
        int SupplierPurchaseDetailsId,
        int ProductVariantId,
        string ProductName,
        string Sku,
        bool IsSerialized,
        int ReceivedQuantity,
        int ReturnedQuantity,
        int RemainingQuantity,
        int InStock,
        decimal UnitPrice,
        decimal NetUnitPrice,
        IReadOnlyList<string> AvailableSerials,
        bool IsReturnable,
        string? NotReturnableReason
    );

    public sealed record PurchaseReturnSummary(int Id, string ReturnNumber, DateTime ReturnDate, decimal TotalAmount, string Status);

    public sealed record PurchaseReturnListItem(
        int Id,
        string ReturnNumber,
        int PurchaseId,
        string InvoiceNumber,
        int SupplierId,
        string SupplierName,
        string SupplierPhoneNumber,
        int BranchId,
        string BranchName,
        DateTime ReturnDate,
        int ItemCount,
        decimal TotalAmount,
        decimal DueReduced,
        decimal SettlementReversed,
        decimal CashRefunded,
        string Status,
        string Reason,
        string? CreatedBy
    );

    public sealed record PurchaseReturnDetail(
        int Id,
        string ReturnNumber,
        int PurchaseId,
        string InvoiceNumber,
        DateTime PurchaseDate,
        int SupplierId,
        string SupplierName,
        string SupplierPhoneNumber,
        int BranchId,
        string BranchName,
        DateTime ReturnDate,
        string Status,
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TotalAmount,
        decimal DueReduced,
        decimal SettlementReversed,
        decimal CashRefunded,
        string Reason,
        string? Remarks,
        string? CreatedBy,
        DateTime CreatedAt,
        DateTime? CancelledAt,
        string? CancelReason,
        IReadOnlyList<PurchaseReturnDetailLine> Lines
    );

    public sealed record PurchaseReturnDetailLine(
        int Id,
        int SupplierPurchaseDetailsId,
        int ProductVariantId,
        string ProductName,
        string Sku,
        int Quantity,
        decimal UnitPrice,
        decimal Amount,
        IReadOnlyList<string> SerialNumbers
    );

    // ---------------------------------------------------------------- endpoints
    public class PurchaseReturnQueryEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-purchase-returnable/{purchaseId:int}", async (int purchaseId, IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetPurchaseReturnableQuery(purchaseId))))
                .WithTags("Purchase Return").RequirePermission(Permissions.PurchasesView);

            app.MapGet("/get-purchase-returns", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                string? search = null,
                string? status = null,
                int? branchId = null,
                int? purchaseId = null,
                DateTime? startDate = null,
                DateTime? endDate = null) =>
                Results.Ok(await mediator.Send(new GetPurchaseReturnsQuery(
                    pageNumber, pageSize, search, status, branchId, purchaseId, startDate, endDate))))
                .WithTags("Purchase Return").RequirePermission(Permissions.PurchasesView);

            app.MapGet("/get-purchase-return/{id:int}", async (int id, IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetPurchaseReturnByIdQuery(id))))
                .WithTags("Purchase Return").RequirePermission(Permissions.PurchasesView);
        }
    }

    // ---------------------------------------------------------------- handlers
    public class GetPurchaseReturnableHandler(AppDbContext _db, ILogger<GetPurchaseReturnableHandler> _logger)
        : IRequestHandler<GetPurchaseReturnableQuery, Result>
    {
        public async Task<Result> Handle(GetPurchaseReturnableQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var purchase = await _db.SupplierPurchases
                    .AsNoTracking()
                    .Include(p => p.Supplier)
                    .Include(p => p.Branch)
                    .Include(p => p.SupplierPurchaseDetails).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
                    .FirstOrDefaultAsync(p => p.Id == request.PurchaseId && p.IsActive != (int)EntityConstant.EntityStatus.Deleted, cancellationToken);
                if (purchase == null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Purchase not found." };

                var eligibility = await PurchaseReturnEligibility.ForPurchaseAsync(_db, purchase, cancellationToken);
                var settled = await _db.SupplierPurchasePayments
                    .IgnoreQueryFilters()
                    .Where(a => a.SupplierPurchaseId == purchase.Id && a.SupplierPayment.PaymentMethod == PartySettlement.PaymentMethod)
                    .SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m;
                var previous = await _db.PurchaseReturns
                    .AsNoTracking()
                    .Where(r => r.PurchaseId == purchase.Id)
                    .OrderBy(r => r.Id)
                    .Select(r => new PurchaseReturnSummary(r.Id, r.ReturnNumber, r.ReturnDate, r.TotalAmount, r.Status.ToString()))
                    .ToListAsync(cancellationToken);

                var billGross = purchase.TotalAmount + purchase.DiscountAmount;
                var lines = purchase.SupplierPurchaseDetails
                    .Where(d => d.Status != LineStatus.Rejected)
                    .OrderBy(d => d.Id)
                    .Select(d =>
                    {
                        var rule = eligibility[d.Id];
                        return new PurchaseReturnableLine(
                            d.Id, d.ProductVariantId, d.ProductVariant.Product.ProductName, d.ProductVariant.SKU,
                            d.ProductVariant.IsSerialized, rule.Received, rule.Returned, rule.Remaining, rule.InStock,
                            d.UnitPrice, ReturnMath.Share(d.UnitPrice, billGross, purchase.TotalAmount),
                            rule.AvailableSerials, rule.CanReturn, rule.BlockedReason);
                    })
                    .ToList();

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Returnable lines retrieved successfully",
                    Data = new PurchaseReturnableResponse(
                        purchase.Id, purchase.InvoiceNumber, purchase.PurchaseDate, purchase.Status.ToString(),
                        purchase.PurchaseType?.ToString(), purchase.SupplierId, purchase.Supplier.Name,
                        purchase.Supplier.PhoneNumber, purchase.BranchId, purchase.Branch.Name,
                        purchase.DiscountAmount, purchase.TotalAmount, purchase.PaidAmount, purchase.DueAmount,
                        purchase.ReturnedAmount, Math.Max(0m, settled), lines, previous)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading returnable lines for purchase {PurchaseId}", request.PurchaseId);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while reading the purchase." };
            }
        }
    }

    public class GetPurchaseReturnsHandler(AppDbContext _db, ILogger<GetPurchaseReturnsHandler> _logger)
        : IRequestHandler<GetPurchaseReturnsQuery, Result>
    {
        public async Task<Result> Handle(GetPurchaseReturnsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _db.PurchaseReturns.AsNoTracking();

                var range = ReportDateRange.From(request.StartDate, request.EndDate);
                if (range.Start is DateTime start)
                    query = query.Where(r => r.ReturnDate >= start);
                if (range.EndExclusive is DateTime end)
                    query = query.Where(r => r.ReturnDate < end);
                if (request.BranchId is int branchId)
                    query = query.Where(r => r.BranchId == branchId);
                if (request.PurchaseId is int purchaseId)
                    query = query.Where(r => r.PurchaseId == purchaseId);
                if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ReturnStatus>(request.Status.Trim(), true, out var status))
                    query = query.Where(r => r.Status == status);
                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    query = query.Where(r =>
                        EF.Functions.ILike(r.ReturnNumber, term) ||
                        EF.Functions.ILike(r.SupplierPurchase.InvoiceNumber, term) ||
                        EF.Functions.ILike(r.Supplier.Name, term) ||
                        EF.Functions.ILike(r.Supplier.PhoneNumber, term));
                }

                var page = await query
                    .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id)
                    .Select(r => new PurchaseReturnListItem(
                        r.Id, r.ReturnNumber, r.PurchaseId, r.SupplierPurchase.InvoiceNumber, r.SupplierId, r.Supplier.Name,
                        r.Supplier.PhoneNumber, r.BranchId, r.Branch.Name, r.ReturnDate,
                        r.Lines.Sum(l => l.Quantity), r.TotalAmount, r.DueReduced, r.SettlementReversed,
                        r.CashRefunded, r.Status.ToString(), r.Reason,
                        _db.Users.Where(u => u.Id == r.CreatedById).Select(u => u.Name).FirstOrDefault()))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Purchase returns retrieved successfully", Data = page };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing purchase returns");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while listing purchase returns." };
            }
        }
    }

    public class GetPurchaseReturnByIdHandler(AppDbContext _db, ILogger<GetPurchaseReturnByIdHandler> _logger)
        : IRequestHandler<GetPurchaseReturnByIdQuery, Result>
    {
        public async Task<Result> Handle(GetPurchaseReturnByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var detail = await _db.PurchaseReturns
                    .AsNoTracking()
                    .Where(r => r.Id == request.Id)
                    .Select(r => new PurchaseReturnDetail(
                        r.Id, r.ReturnNumber, r.PurchaseId, r.SupplierPurchase.InvoiceNumber, r.SupplierPurchase.PurchaseDate,
                        r.SupplierId, r.Supplier.Name, r.Supplier.PhoneNumber,
                        r.BranchId, r.Branch.Name, r.ReturnDate, r.Status.ToString(),
                        r.SubTotal, r.DiscountAmount, r.TotalAmount,
                        r.DueReduced, r.SettlementReversed, r.CashRefunded, r.Reason, r.Remarks,
                        _db.Users.Where(u => u.Id == r.CreatedById).Select(u => u.Name).FirstOrDefault(),
                        r.CreatedAt, r.CancelledAt, r.CancelReason,
                        r.Lines.OrderBy(l => l.Id).Select(l => new PurchaseReturnDetailLine(
                            l.Id, l.SupplierPurchaseDetailsId, l.ProductVariantId, l.ProductVariant.Product.ProductName,
                            l.ProductVariant.SKU, l.Quantity, l.UnitPrice, l.Amount,
                            l.Serials.Select(s => s.ProductSerial.SerialNumber).ToList())).ToList()))
                    .FirstOrDefaultAsync(cancellationToken);

                return detail == null
                    ? new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Return not found." }
                    : new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Purchase return retrieved successfully", Data = detail };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading purchase return {Id}", request.Id);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while reading the return." };
            }
        }
    }
}
