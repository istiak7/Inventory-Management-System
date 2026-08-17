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
        // Contact details, carried so an account statement / receipt can be printed
        // without a second round-trip to the customer list.
        string PhoneNumber,
        string Email,
        string Address,
        decimal OpeningBalance
    );
}
