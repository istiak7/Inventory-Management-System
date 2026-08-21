namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public sealed record SupplierPaymentHistoryResponse(
        int Id,
        int PaymentId,
        int InvoiceId,
        string? InvoiceNumber,
        decimal Amount,
        DateTime AllocationDate,
        string PaymentMethod,
        string? Remarks,
        DateTime PaymentDate,
        int SupplierId,
        string SupplierName,
        decimal PaymentAmount,
        decimal BalanceBefore,
        decimal BalanceAfter,
        decimal InvoiceTotalAmount,
        decimal InvoiceDueBefore,
        decimal InvoiceDueAfter
    );
}
