namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public sealed record SalePaymentResponse(
        int Id,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod
    );
}
