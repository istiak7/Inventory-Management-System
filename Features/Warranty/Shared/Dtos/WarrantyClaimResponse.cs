namespace Inventory_Management_System.Features.Warranty.Shared.Dtos
{
    /// <summary>
    /// One warranty claim, whole. The list and the detail screen share this shape on purpose —
    /// every field below comes from the same joins, so splitting it would buy two mappers and no
    /// query savings.
    /// </summary>
    public sealed record WarrantyClaimResponse(
        int Id,
        string ClaimNumber,
        DateTime ClaimDate,
        string Status,
        string? Resolution,

        int ProductSerialId,
        string SerialNumber,
        int ProductVariantId,
        string Sku,
        string ProductName,

        int CustomerId,
        string CustomerName,
        string CustomerPhoneNumber,
        string CustomerAddress,

        int BranchId,
        string BranchName,

        int SaleId,
        int SaleDetailsId,
        string InvoiceNumber,
        DateTime SaleDate,
        int WarrantyMonths,
        DateTime WarrantyExpiryDate,

        string DefectDescription,
        string? AccessoriesReceived,
        string? TechnicianName,
        string? ResolutionNotes,
        int? ReplacementSerialId,
        string? ReplacementSerialNumber,

        DateTime? RepairStartedAt,
        DateTime? ResolvedAt,
        DateTime? RejectedAt,
        DateTime? DeliveredAt);
}
