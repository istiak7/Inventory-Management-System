namespace Inventory_Management_System.Features.Stocks.Shared.Dtos
{
    /// <summary>One physical serialized unit, read straight from ProductSerial.</summary>
    public sealed record SerialResponse(
        int Id,
        string SerialNumber,
        int ProductVariantId,
        string ProductName,
        string SKU,
        int BranchId,
        string BranchName,
        // SerialStatus name: Ordered, InStock, Sold, RmaReturned or Defective.
        string Status,
        DateTime ReceivedDate,
        DateTime? SoldDate,
        int WarrantyDays,
        int? PurchaseId,
        string? PurchaseReference,
        int? SaleId,
        string? SaleReference
    );
}
