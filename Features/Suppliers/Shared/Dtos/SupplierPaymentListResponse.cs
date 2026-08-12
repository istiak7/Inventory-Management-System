namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    /// <summary>A single supplier payment (used to populate the payment filter dropdown).</summary>
    public sealed record SupplierPaymentListResponse(
        int Id,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod,
        string? Remarks
    );
}
