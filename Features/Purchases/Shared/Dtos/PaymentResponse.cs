namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record PaymentResponse(
        int Id,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod
    );
}
