namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    /// <summary>
    /// One row of a delivery challan: a product handed over, with every serial delivered under it.
    /// No prices — a challan accompanies the goods, the invoice carries the money.
    /// </summary>
    public sealed record ChallanLineResponse(
        int SN,
        string ProductDescription,
        IReadOnlyList<string> SerialNumbers,
        int Quantity,
        string Uom
    );

    public sealed record GetSaleChallanResponse(
        int SaleId,
        // The challan is numbered after the sale it delivers, as the reference challan is.
        string ChallanNumber,
        string InvoiceNumber,
        DateTime SaleDate,
        string CustomerName,
        string CustomerAddress,
        string MobileNumber,
        string BranchName,
        // The user who entered the sale; null for sales recorded before audit stamping.
        string? SoldBy,
        IReadOnlyList<ChallanLineResponse> Items,
        int TotalQuantity
    );
}
