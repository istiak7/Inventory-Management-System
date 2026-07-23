namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record PurchaseOrderLineResponse(
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal TotalAmount,
        string IsApproved
    );
}
