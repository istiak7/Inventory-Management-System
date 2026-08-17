namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    /// <summary>
    /// One payment-to-invoice allocation: how much of a payment was applied to which invoice.
    /// Also carries the surrounding balances so a money receipt can be printed for this single
    /// allocation without recomputing the ledger on the client.
    /// </summary>
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
        /// <summary>Total of the whole payment, which may cover several invoices plus on-account credit.</summary>
        decimal PaymentAmount,
        /// <summary>Customer's receivable balance immediately before this payment hit the ledger.</summary>
        decimal BalanceBefore,
        /// <summary>Customer's receivable balance immediately after this payment hit the ledger.</summary>
        decimal BalanceAfter,
        decimal InvoiceTotalAmount,
        /// <summary>The invoice's outstanding due just before this allocation was applied.</summary>
        decimal InvoiceDueBefore,
        /// <summary>The invoice's outstanding due just after this allocation was applied.</summary>
        decimal InvoiceDueAfter
    );
}
