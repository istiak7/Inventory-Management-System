namespace Inventory_Management_System.Features.Transfers.Shared.Dtos
{
    // Shared by the list and the by-id query, same shape both times (mirrors PurchaseOrderListResponse).
    public sealed record StockTransferResponse(
        int Id,
        string Reference,
        int SourceBranchId,
        string SourceBranchName,
        int DestinationBranchId,
        string DestinationBranchName,
        string Status,
        string? Notes,
        DateTime CreatedAt,
        DateTime? SubmittedAt,
        DateTime? ApprovedAt,
        DateTime? RejectedAt,
        int ItemsCount,
        int TotalUnits,
        IReadOnlyList<StockTransferLineResponse> Items
    );
}
