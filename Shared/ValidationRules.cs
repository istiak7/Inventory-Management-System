using FluentValidation;

namespace Inventory_Management_System.Shared
{
    /// <summary>Common validation rules, so every command checks money and dates the same way.</summary>
    public static class ValidationRules
    {
        /// <summary>Money has at most 2 decimals (the database stores 18,2).</summary>
        public static IRuleBuilderOptions<T, decimal> Money<T>(this IRuleBuilder<T, decimal> rule) =>
            rule.PrecisionScale(18, 2, true).WithMessage("{PropertyName} can have at most 2 decimals.");

        public static IRuleBuilderOptions<T, decimal?> Money<T>(this IRuleBuilder<T, decimal?> rule) =>
            rule.PrecisionScale(18, 2, true).WithMessage("{PropertyName} can have at most 2 decimals.");

        /// <summary>
        /// A business date (sale, purchase, payment) cannot be in the future.
        /// One day of slack covers time zone differences between the browser and the server.
        /// </summary>
        public static IRuleBuilderOptions<T, DateTime?> NotInFuture<T>(this IRuleBuilder<T, DateTime?> rule) =>
            rule.Must(d => d is null || d.Value.ToUniversalTime() <= DateTime.UtcNow.AddDays(1))
                .WithMessage("{PropertyName} cannot be in the future.");
    }
}
