using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Reports.Shared
{
    public readonly record struct ReportDateRange(DateTime? Start, DateTime? EndExclusive)
    {
        // The dates a user picks are shop calendar days; the stored moments are UTC.
        public static ReportDateRange From(DateTime? startDate, DateTime? endDate) =>
            new(
                startDate is DateTime start ? BusinessClock.StartOfDayUtc(start) : null,
                endDate is DateTime end ? BusinessClock.StartOfDayUtc(end.Date.AddDays(1)) : null);

        public bool Covers(DateTime value) =>
            (Start is null || value >= Start.Value) &&
            (EndExclusive is null || value < EndExclusive.Value);
    }
}
