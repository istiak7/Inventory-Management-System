namespace Inventory_Management_System.Shared
{
    /// <summary>
    /// The shop's own time zone (config "BusinessTimeZone", default "Asia/Dhaka").
    /// Dates are stored in UTC, but a "day" in a report, the dashboard or a date filter means the
    /// shop's calendar day: a sale at 02:00 in Dhaka belongs to that day, not to the day before.
    /// </summary>
    public static class BusinessClock
    {
        private static TimeZoneInfo _zone = TimeZoneInfo.Utc;

        public static TimeZoneInfo Zone => _zone;

        public static void Configure(string? timeZoneId)
        {
            if (!string.IsNullOrWhiteSpace(timeZoneId))
                _zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }

        /// <summary>Today's date in the shop's time zone.</summary>
        public static DateTime Today => ToLocal(DateTime.UtcNow).Date;

        /// <summary>The UTC moment the given shop calendar day starts.</summary>
        public static DateTime StartOfDayUtc(DateTime localDate) =>
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified), _zone);

        /// <summary>A stored (UTC) moment as shop local time.</summary>
        public static DateTime ToLocal(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _zone);

        /// <summary>The shop calendar day a stored (UTC) moment falls on.</summary>
        public static DateOnly LocalDateOf(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc));
    }
}
