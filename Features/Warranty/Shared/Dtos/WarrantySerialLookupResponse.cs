namespace Inventory_Management_System.Features.Warranty.Shared.Dtos
{
    public sealed record WarrantySerialLookupResponse(
        int ProductSerialId,
        string SerialNumber,
        string SerialStatus,
        int ProductVariantId,
        string Sku,
        string ProductName,
        int BranchId,
        string BranchName,
        DateTime? SoldDate,
        int WarrantyMonths,
        DateTime? WarrantyExpiryDate,
        bool IsUnderWarranty,
        int DaysRemaining,
        bool CanOpenClaim,
        string EligibilityMessage,
        WarrantySaleReference? Sale,
        IReadOnlyList<WarrantyClaimResponse> Claims
    );

    public sealed record WarrantySaleReference(
        int SaleId,
        string InvoiceNumber,
        DateTime SaleDate,
        int SaleDetailsId,
        int CustomerId,
        string CustomerName,
        string CustomerPhoneNumber,
        bool IsReplacementUnit,
        string? ReplacedSerialNumber
    );
}
