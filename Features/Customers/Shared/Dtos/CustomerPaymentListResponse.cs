namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    /// <summary>A single customer payment (used to populate the payment filter dropdown).</summary>
    public sealed record CustomerPaymentListResponse(
        int Id,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod,
        string? Remarks
    );
}
