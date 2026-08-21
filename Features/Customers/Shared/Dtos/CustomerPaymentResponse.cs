namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public sealed record CustomerPaymentResponse(
        int Id,
        int CustomerId,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod,
        string? Remarks,
        decimal AllocatedAmount,
        decimal BalanceAfter
    );
}
