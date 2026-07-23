namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public sealed record SupplierPaymentResponse(
        int Id,
        int SupplierId,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod,
        decimal AllocatedAmount,   // portion applied to outstanding invoices
        decimal BalanceAfter       // supplier's overall balance after this payment
    );
}
