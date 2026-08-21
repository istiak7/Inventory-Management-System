namespace Inventory_Management_System.Features.Warranty.Shared.Dtos
{
    public sealed record ReplacementCandidateResponse(
        int ProductSerialId,
        string SerialNumber,
        int ProductVariantId,
        string Sku,
        string ProductName,
        int BranchId,
        string BranchName,
        DateTime ReceivedDate,
        int WarrantyMonths
    );
}
