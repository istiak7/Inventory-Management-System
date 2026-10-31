namespace Inventory_Management_System.Shared
{
    /// <summary>
    /// The ways money is paid or received. The payment forms offer exactly these
    /// (src/constants/payment-methods.ts in the frontend), and the Payment report filters by them,
    /// so a payment recorded with any other method would be invisible to that filter.
    /// </summary>
    public static class PaymentMethods
    {
        public static readonly IReadOnlyList<string> All = ["Cash", "Bank", "bKash", "Nagad", "Rocket", "Cheque"];

        public static bool IsKnown(string? method) => method is not null && All.Contains(method);
    }
}
