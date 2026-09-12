using Inventory_Management_System.Shared.Extensions.PaginationExtensions;

namespace Inventory_Management_System.Features.Reports.Shared.Dtos
{
    public sealed record StockDateWiseRow(
        int BranchId,
        string BranchName,
        int ProductVariantId,
        string SKU,
        string ProductName,
        string CategoryName,
        string SubCategoryName,
        int OpeningStock,
        int PurchaseInQuantity,
        int ReturnInQuantity,
        int TransferInQuantity,
        int SaleOutQuantity,
        int ReturnOutQuantity,
        int TransferOutQuantity,
        int DamageOutQuantity,
        int AdjustmentQuantity,
        int StockInQuantity,
        int StockOutQuantity,
        int ClosingStock
    );

    public sealed record StockDateWiseSummary(
        long ItemCount,
        int OpeningStock,
        int StockInQuantity,
        int StockOutQuantity,
        int AdjustmentQuantity,
        int ClosingStock
    );

    public sealed record StockDateWiseReportResponse(
        StockDateWiseSummary Summary,
        PagedResult<StockDateWiseRow> Rows
    );
}
