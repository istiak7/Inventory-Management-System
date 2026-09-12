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

    public static class ReportPaymentStatuses
    {
        public const string Allocated = "Allocated";
        public const string PartiallyAllocated = "PartiallyAllocated";
        public const string Unallocated = "Unallocated";

        public static string For(decimal amount, decimal allocatedAmount)
        {
            if (allocatedAmount <= 0m) return Unallocated;
            return allocatedAmount >= amount ? Allocated : PartiallyAllocated;
        }
    }
}
