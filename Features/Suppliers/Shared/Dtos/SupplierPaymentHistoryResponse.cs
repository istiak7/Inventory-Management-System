namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    /// <summary>
    /// One payment-to-invoice allocation: how much of a payment was applied to which invoice.
    /// </summary>
    public sealed record SupplierPaymentHistoryResponse(
        int Id,
        int PaymentId,
        int InvoiceId,
        string? InvoiceNumber,
        decimal Amount,
        DateTime AllocationDate,
        string PaymentMethod,
        DateTime PaymentDate
    );
}
