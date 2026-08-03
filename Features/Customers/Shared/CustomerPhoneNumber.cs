namespace Inventory_Management_System.Features.Customers.Shared
{
    /// <summary>
    /// Canonical form of a customer's mobile number. The phone number is what identifies a
    /// customer (email is optional for walk-ins), so every write and every lookup has to agree on
    /// what "the same number" means — otherwise "01712-345678" and "+8801712345678" become two
    /// customers for one person and the unique index never fires.
    /// </summary>
    public static class CustomerPhoneNumber
    {
        /// <summary>Shortest input we accept as a phone number at all.</summary>
        public const int MinimumDigits = 11;

        /// <summary>
        /// Reduces a typed number to the form stored in <c>Customers.PhoneNumber</c>:
        /// digits only, with a Bangladesh country code collapsed to the local 0-leading form
        /// (<c>+880 1712-345678</c>, <c>008801712345678</c> and <c>01712345678</c> all become
        /// <c>01712345678</c>). Anything that is not a BD number is left as its digits, so
        /// foreign numbers still round-trip — they simply compare digit for digit.
        /// </summary>
        public static string Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var digits = new string(raw.Where(char.IsDigit).ToArray());

            // "00" is the international dialling prefix — drop it so 0088… and +88… converge.
            if (digits.StartsWith("00"))
                digits = digits[2..];

            // 880 + a 10-digit local subscriber number is the BD mobile format. Store it the way
            // people actually type it locally: a leading 0 instead of the country code.
            if (digits.Length == 13 && digits.StartsWith("880"))
                digits = "0" + digits[3..];

            return digits;
        }

        /// <summary>True when the number has enough digits to be worth looking up or storing.</summary>
        public static bool IsValid(string? raw) => Normalize(raw).Length >= MinimumDigits;
    }
}
