using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Warranty.Shared.Dtos;

namespace Inventory_Management_System.Features.Warranty.Shared
{
    /// <summary>
    /// Flat row a claim is read into. It exists only to carry the enums out of SQL intact —
    /// EF cannot translate Enum.ToString(), so the stringifying happens in <see cref="ToResponse"/>
    /// after materialization (same shape as the Sales query handlers).
    /// </summary>
    public sealed record WarrantyClaimRow(
        int Id,
        string ClaimNumber,
        DateTime ClaimDate,
        WarrantyClaimStatus Status,
        WarrantyResolutionType? Resolution,
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

    /// <summary>
    /// Every read path — list, detail, and the row each command echoes back — goes through this
    /// projection, so a claim looks identical wherever it surfaces.
    /// </summary>
    public static class WarrantyClaimProjection
    {
        public static IQueryable<WarrantyClaimRow> ProjectToRow(this IQueryable<WarrantyClaim> query) =>
            query.Select(c => new WarrantyClaimRow(
                c.Id,
                c.ClaimNumber,
                c.ClaimDate,
                c.Status,
                c.Resolution,
                c.ProductSerialId,
                c.ProductSerial.SerialNumber,
                c.ProductSerial.ProductVariantId,
                c.ProductSerial.ProductVariant.SKU,
                c.ProductSerial.ProductVariant.Product.ProductName,
                c.CustomerId,
                c.Customer.Name,
                c.Customer.PhoneNumber,
                c.Customer.Address,
                c.BranchId,
                c.Branch.Name,
                c.SaleDetails.SaleId,
                c.SaleDetailsId,
                c.SaleDetails.CustomerSale.InvoiceNumber,
                c.SaleDetails.CustomerSale.SaleDate,
                c.SaleDetails.WarrantyMonths ?? 0,
                c.WarrantyExpiryDate,
                c.DefectDescription,
                c.AccessoriesReceived,
                c.TechnicianName,
                c.ResolutionNotes,
                c.ReplacementSerialId,
                c.ReplacementSerial != null ? c.ReplacementSerial.SerialNumber : null,
                c.RepairStartedAt,
                c.ResolvedAt,
                c.RejectedAt,
                c.DeliveredAt));

        public static WarrantyClaimResponse ToResponse(this WarrantyClaimRow row) =>
            new(row.Id, row.ClaimNumber, row.ClaimDate, row.Status.ToString(), row.Resolution?.ToString(),
                row.ProductSerialId, row.SerialNumber, row.ProductVariantId, row.Sku, row.ProductName,
                row.CustomerId, row.CustomerName, row.CustomerPhoneNumber, row.CustomerAddress,
                row.BranchId, row.BranchName,
                row.SaleId, row.SaleDetailsId, row.InvoiceNumber, row.SaleDate,
                row.WarrantyMonths, row.WarrantyExpiryDate,
                row.DefectDescription, row.AccessoriesReceived, row.TechnicianName, row.ResolutionNotes,
                row.ReplacementSerialId, row.ReplacementSerialNumber,
                row.RepairStartedAt, row.ResolvedAt, row.RejectedAt, row.DeliveredAt);
    }
}
