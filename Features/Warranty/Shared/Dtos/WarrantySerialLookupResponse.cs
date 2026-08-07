namespace Inventory_Management_System.Features.Warranty.Shared.Dtos
{
    /// <summary>
    /// The eligibility card the counter sees after scanning a serial. It answers the whole
    /// question in one payload: what the unit is, who bought it and on which invoice, whether it
    /// is still covered, and whether a claim can be opened right now.
    /// </summary>
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
        // False for an expired unit, one that was never sold, or one already sitting on an open
        // claim. EligibilityMessage says which, so the UI never has to guess a reason.
        bool CanOpenClaim,
        string EligibilityMessage,
        WarrantySaleReference? Sale,
        IReadOnlyList<WarrantyClaimResponse> Claims);

    /// <summary>Where this unit's cover comes from — its own sale, or the swap that issued it.</summary>
    public sealed record WarrantySaleReference(
        int SaleId,
        string InvoiceNumber,
        DateTime SaleDate,
        int SaleDetailsId,
        int CustomerId,
        string CustomerName,
        string CustomerPhoneNumber,
        // True when this unit was handed over as a warranty replacement rather than sold directly,
        // in which case the invoice above is the ORIGINAL unit's.
        bool IsReplacementUnit,
        string? ReplacedSerialNumber);
}
