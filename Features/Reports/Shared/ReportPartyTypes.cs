namespace Inventory_Management_System.Features.Reports.Shared
{
    public static class ReportPartyTypes
    {
        public const string All = "All";
        public const string Customer = "Customer";
        public const string Supplier = "Supplier";

        public static bool IsAll(string? partyType) =>
            string.IsNullOrWhiteSpace(partyType) ||
            partyType.Equals(All, StringComparison.OrdinalIgnoreCase);

        public static bool IncludesCustomer(string? partyType) =>
            IsAll(partyType) || partyType!.Equals(Customer, StringComparison.OrdinalIgnoreCase);

        public static bool IncludesSupplier(string? partyType) =>
            IsAll(partyType) || partyType!.Equals(Supplier, StringComparison.OrdinalIgnoreCase);

        public static bool IsValid(string? partyType) =>
            IsAll(partyType) || IncludesCustomer(partyType) || IncludesSupplier(partyType);
    }
}
