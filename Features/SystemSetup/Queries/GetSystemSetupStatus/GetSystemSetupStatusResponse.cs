namespace Inventory_Management_System.Features.SystemSetup.Queries.GetSystemSetupStatus
{
    public static class SetupSectionStatus
    {
        public const string Configured = "Configured";
        public const string NotConfigured = "NotConfigured";
        public const string Error = "Error";
    }

    public static class SetupOverallStatus
    {
        public const string NotStarted = "NotStarted";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
    }

    /// <summary>One setup section, e.g. "Branches".</summary>
    public sealed class SetupSectionResponse
    {
        public required string Section { get; init; }
        public required string Name { get; init; }
        public int Order { get; init; }
        public bool IsRequired { get; init; }
        public string Status { get; set; } = SetupSectionStatus.NotConfigured;
        /// <summary>Why the section is not configured (or failed). Null when configured.</summary>
        public string? Message { get; set; }
        /// <summary>Rows that are not deleted. Null for sections that are not a list (company).</summary>
        public int? TotalRecords { get; set; }
        public int? ActiveRecords { get; set; }
        /// <summary>Current saved data, for sections that have it (company).</summary>
        public object? Data { get; set; }
    }

    public sealed record GetSystemSetupStatusResponse(
        string OverallStatus,
        int RequiredSections,
        int ConfiguredRequiredSections,
        // True once any sale or purchase exists: setup is then "live", not a fresh install.
        bool HasTransactions,
        DateTime CheckedAt,
        IReadOnlyList<SetupSectionResponse> Sections);
}
