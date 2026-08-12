namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    /// <summary>
    /// One payment-to-invoice allocation: how much of a payment was applied to which invoice.
    /// Also carries the surrounding balances so a money receipt can be printed for this single
    /// allocation without recomputing the ledger on the client.
    /// </summary>
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
        /// <summary>Total of the whole payment, which may cover several invoices plus on-account credit.</summary>
        decimal PaymentAmount,
        /// <summary>Supplier's payable balance immediately before this payment hit the ledger.</summary>
        decimal BalanceBefore,
        /// <summary>Supplier's payable balance immediately after this payment hit the ledger.</summary>
        decimal BalanceAfter,
        decimal InvoiceTotalAmount,
        /// <summary>The invoice's outstanding due just before this allocation was applied.</summary>
        decimal InvoiceDueBefore,
        /// <summary>The invoice's outstanding due just after this allocation was applied.</summary>
        decimal InvoiceDueAfter
    );
}
