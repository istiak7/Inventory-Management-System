namespace Inventory_Management_System.Features.Reports.Shared.Dtos
{
    public sealed record ReportPaymentAllocation(
        int PaymentId,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod
    );
}
