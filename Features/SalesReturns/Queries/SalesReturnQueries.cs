using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.SalesReturns.Queries
{
    // ---------------------------------------------------------------- requests
    public sealed record GetSaleReturnableQuery(int SaleId) : IRequest<Result>;

    public sealed record GetSalesReturnsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? Search = null,
        string? Status = null,
        int? BranchId = null,
        int? SaleId = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null
    ) : IRequest<Result>;

    public sealed record GetSalesReturnByIdQuery(int Id) : IRequest<Result>;

    // ---------------------------------------------------------------- responses
    public sealed record SaleReturnableResponse(
        int SaleId,
        string InvoiceNumber,
        DateTime SaleDate,
        string Status,
        string SaleType,
        int CustomerId,
        string CustomerName,
        string CustomerPhoneNumber,
        int BranchId,
        string BranchName,
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueAmount,
        decimal ReturnedAmount,
        // Part of PaidAmount that came from a cross-ledger settlement (reversed first on a return).
        decimal SettledAmount,
        IReadOnlyList<SaleReturnableLine> Lines,
        IReadOnlyList<ReturnSummary> PreviousReturns
    );

    public sealed record SaleReturnableLine(
        int SaleDetailsId,
        int ProductVariantId,
        string ProductName,
        string Sku,
        string? SerialNumber,
        int SoldQuantity,
        int ReturnedQuantity,
        int RemainingQuantity,
        decimal UnitPrice,
        // Unit price after this line's share of the invoice discount and tax: what one unit
        // returns. The saved return uses the same rule (rounded on the total).
        decimal NetUnitPrice,
        bool IsReturnable,
        string? NotReturnableReason
    );

    public sealed record ReturnSummary(int Id, string ReturnNumber, DateTime ReturnDate, decimal TotalAmount, string Status);

    public sealed record SalesReturnListItem(
        int Id,
        string ReturnNumber,
        int SaleId,
        string InvoiceNumber,
        int CustomerId,
        string CustomerName,
        string CustomerPhoneNumber,
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

    public sealed record SalesReturnDetail(
        int Id,
        string ReturnNumber,
        int SaleId,
        string InvoiceNumber,
        DateTime SaleDate,
        int CustomerId,
        string CustomerName,
        string CustomerPhoneNumber,
        string CustomerAddress,
        int BranchId,
        string BranchName,
        DateTime ReturnDate,
        string Status,
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxAmount,
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
        IReadOnlyList<SalesReturnDetailLine> Lines
    );

    public sealed record SalesReturnDetailLine(
        int Id,
        int SaleDetailsId,
        int ProductVariantId,
        string ProductName,
        string Sku,
        string? SerialNumber,
        int Quantity,
        decimal UnitPrice,
        decimal Amount,
        bool Restock
    );

    // ---------------------------------------------------------------- endpoints
    public class SalesReturnQueryEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-sale-returnable/{saleId:int}", async (int saleId, IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetSaleReturnableQuery(saleId))))
                .WithTags("Sales Return").RequirePermission(Permissions.SalesView);

            app.MapGet("/get-sales-returns", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                string? search = null,
                string? status = null,
                int? branchId = null,
                int? saleId = null,
                DateTime? startDate = null,
                DateTime? endDate = null) =>
                Results.Ok(await mediator.Send(new GetSalesReturnsQuery(
                    pageNumber, pageSize, search, status, branchId, saleId, startDate, endDate))))
                .WithTags("Sales Return").RequirePermission(Permissions.SalesView);

            app.MapGet("/get-sales-return/{id:int}", async (int id, IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetSalesReturnByIdQuery(id))))
                .WithTags("Sales Return").RequirePermission(Permissions.SalesView);
        }
    }
}
