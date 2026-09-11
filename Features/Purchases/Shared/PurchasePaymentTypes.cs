using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Features.Purchases.Shared
{
    // Cash and Debit are the only payment types a purchase can carry. Parsing lives here so the
    // create and update slices reject the same values and report the same message.
    public static class PurchasePaymentTypes
    {
        // Listed in error messages so the caller can see what is accepted.
        public static readonly string Allowed = string.Join(", ", Enum.GetNames<PurchaseType>());

        // True only for an exact (case-insensitive) enum name. Numbers like "1" and retired
        // options like "Full", "Partial", "Due" and "Credit" are rejected.
        public static bool TryParse(string? value, out PurchaseType paymentType)
        {
            paymentType = default;

            var name = value?.Trim();
            if (string.IsNullOrEmpty(name))
                return false;

            return Enum.TryParse(name, ignoreCase: true, out paymentType)
                && string.Equals(Enum.GetName(paymentType), name, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValid(string? value) => TryParse(value, out _);
    }
}
