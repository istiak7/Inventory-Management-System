namespace Inventory_Management_System.Features.Warranty.Shared.Dtos
{
    /// <summary>
    /// An in-stock unit that could be handed over as a replacement: same variant, same branch,
    /// still <c>InStock</c>. Oldest stock first, so a swap clears the shelf in the order it arrived.
    /// </summary>
    public sealed record ReplacementCandidateResponse(
        int ProductSerialId,
        string SerialNumber,
        int ProductVariantId,
        string Sku,
        string ProductName,
        int BranchId,
        string BranchName,
        DateTime ReceivedDate,
        int WarrantyMonths);
}
