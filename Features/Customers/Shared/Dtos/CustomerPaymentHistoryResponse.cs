namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public sealed record CustomerPaymentHistoryResponse(
        int Id,
        int PaymentId,
        int InvoiceId,
        string? InvoiceNumber,
        decimal Amount,
        DateTime AllocationDate,
        string PaymentMethod,
        string? Remarks,
        DateTime PaymentDate,
        int CustomerId,
        string CustomerName,
        decimal PaymentAmount,
        decimal BalanceBefore,
        decimal BalanceAfter,
        decimal InvoiceTotalAmount,
        decimal InvoiceDueBefore,
        decimal InvoiceDueAfter
    );
}
