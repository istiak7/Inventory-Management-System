namespace Inventory_Management_System.Features.Transfers.Shared.Dtos
{
    public sealed record StockTransferLineResponse(
        int Id,
        int ProductVariantId,
        string Sku,
        string ProductName,
        bool IsSerialized,
        int Quantity,
        IReadOnlyList<string> SerialNumbers
    );
}
