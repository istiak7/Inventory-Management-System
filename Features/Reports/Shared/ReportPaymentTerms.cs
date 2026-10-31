namespace Inventory_Management_System.Features.Reports.Shared
{
    public static class ReportPaymentDirections
    {
        public const string All = "All";
        public const string Received = "Received";
        public const string Paid = "Paid";

        public static bool IsAll(string? direction) =>
            string.IsNullOrWhiteSpace(direction) ||
            direction.Equals(All, StringComparison.OrdinalIgnoreCase);

        public static bool IncludesReceived(string? direction) =>
            IsAll(direction) || direction!.Equals(Received, StringComparison.OrdinalIgnoreCase);

        public static bool IncludesPaid(string? direction) =>
            IsAll(direction) || direction!.Equals(Paid, StringComparison.OrdinalIgnoreCase);

        public static bool IsValid(string? direction) =>
            IsAll(direction) || IncludesReceived(direction) || IncludesPaid(direction);
    }

    /// <summary>
    /// Sales and purchases are Cash or Credit. Credit is stored as the "Debit" SaleType /
    /// PurchaseType, so exports print it under the name the shop uses.
    /// </summary>
    public static class ReportPaymentTerms
    {
        public static string Label(string? type) =>
            string.Equals(type, "Debit", StringComparison.OrdinalIgnoreCase) ? "Credit" : type ?? string.Empty;
    }

    public static class ReportPaymentStatuses
    {
        public const string Allocated = "Allocated";
        public const string PartiallyAllocated = "PartiallyAllocated";
        public const string Unallocated = "Unallocated";

        /// <summary>An office expense: spent in full when recorded, with no invoice to allocate to.</summary>
        public const string Expense = "Expense";

        public static string For(decimal amount, decimal allocatedAmount)
        {
            if (allocatedAmount <= 0m) return Unallocated;
            return allocatedAmount >= amount ? Allocated : PartiallyAllocated;
        }
    }
}
