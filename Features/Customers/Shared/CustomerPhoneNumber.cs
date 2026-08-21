namespace Inventory_Management_System.Features.Customers.Shared
{
    public static class CustomerPhoneNumber
    {
        public const int MinimumDigits = 11;

        public static string Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var digits = new string(raw.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("00"))
                digits = digits[2..];

            if (digits.Length == 13 && digits.StartsWith("880"))
                digits = "0" + digits[3..];

            return digits;
        }

        public static bool IsValid(string? raw) => Normalize(raw).Length >= MinimumDigits;
    }
}
