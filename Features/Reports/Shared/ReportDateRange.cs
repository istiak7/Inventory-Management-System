namespace Inventory_Management_System.Features.Reports.Shared
{
    public readonly record struct ReportDateRange(DateTime? Start, DateTime? EndExclusive)
    {
        public static ReportDateRange From(DateTime? startDate, DateTime? endDate) =>
            new(startDate?.Date, endDate?.Date.AddDays(1));

        public bool Covers(DateTime value) =>
            (Start is null || value >= Start.Value) &&
            (EndExclusive is null || value < EndExclusive.Value);
    }
}
