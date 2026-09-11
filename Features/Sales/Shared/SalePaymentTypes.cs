using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Features.Sales.Shared
{
    // Cash and Debit are the only payment types a sale can carry. Parsing lives here so the
    // endpoint, the validator and the handler all reject the same values with the same message.
    public static class SalePaymentTypes
    {
        // Listed in error messages so the caller can see what is accepted.
        public static readonly string Allowed = string.Join(", ", Enum.GetNames<SaleType>());

        // True only for an exact (case-insensitive) enum name. Numbers like "1" and retired
        // options like "Full", "Partial", "Due" and "Credit" are rejected.
        public static bool TryParse(string? value, out SaleType paymentType)
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
