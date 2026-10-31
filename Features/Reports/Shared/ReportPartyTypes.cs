namespace Inventory_Management_System.Features.Reports.Shared
{
    public static class ReportPartyTypes
    {
        public const string All = "All";
        public const string Customer = "Customer";
        public const string Supplier = "Supplier";

        /// <summary>
        /// Office expenses, as the third kind of money movement the payment report lists. Only the
        /// payment report accepts it — <see cref="IsValid"/> deliberately does not, so the ledger
        /// and party reports keep rejecting a party type they have no rows for.
        /// </summary>
        public const string Expense = "Expense";

        public static bool IsAll(string? partyType) =>
            string.IsNullOrWhiteSpace(partyType) ||
            partyType.Equals(All, StringComparison.OrdinalIgnoreCase);

        public static bool IncludesCustomer(string? partyType) =>
            IsAll(partyType) || partyType!.Equals(Customer, StringComparison.OrdinalIgnoreCase);

        public static bool IncludesSupplier(string? partyType) =>
            IsAll(partyType) || partyType!.Equals(Supplier, StringComparison.OrdinalIgnoreCase);

        public static bool IncludesExpense(string? partyType) =>
            IsAll(partyType) || partyType!.Equals(Expense, StringComparison.OrdinalIgnoreCase);

        public static bool IsExpense(string? partyType) =>
            !IsAll(partyType) && partyType!.Equals(Expense, StringComparison.OrdinalIgnoreCase);

        public static bool IsValid(string? partyType) =>
            IsAll(partyType) || IncludesCustomer(partyType) || IncludesSupplier(partyType);
    }
}
