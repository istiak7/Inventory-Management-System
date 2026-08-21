namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public sealed record CustomerAccountResponse(
        int CustomerId,
        string CustomerName,
        string Group,
        decimal TotalSales,
        decimal TotalPayments,
        decimal Balance,
        DateTime? LastTransactionDate,
        string PhoneNumber,
        string Email,
        string Address,
        decimal OpeningBalance
    );
}
