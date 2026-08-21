namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public sealed record SupplierPaymentListResponse(
        int Id,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod,
        string? Remarks
    );
}
