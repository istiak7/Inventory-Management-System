namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public sealed record CustomerPaymentListResponse(
        int Id,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod,
        string? Remarks
    );
}
