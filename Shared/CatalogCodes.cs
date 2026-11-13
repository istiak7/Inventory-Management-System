using System.Text;

namespace Inventory_Management_System.Shared
{
    /// <summary>
    /// Builds the Code (category / sub-category) and SKU (variant) that the catalog forms no longer
    /// ask for. They are derived from names already in the catalog and made unique, so the unique
    /// SKU index and the bulk-upload lookups by code keep working.
    /// </summary>
    public static class CatalogCodes
    {
        public const int CodeMaxLength = 50;
        public const int SkuMaxLength = 50;

        /// <summary>Upper-cased letters and digits of the text, words joined by '-', cut to the length.</summary>
        public static string Slug(string? text, int maxLength)
        {
            var sb = new StringBuilder();
            var pendingDash = false;
            foreach (var ch in text ?? string.Empty)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    if (pendingDash && sb.Length > 0) sb.Append('-');
                    pendingDash = false;
                    sb.Append(char.ToUpperInvariant(ch));
                }
                else pendingDash = true;
            }
            var slug = sb.ToString();
            return slug.Length > maxLength ? slug[..maxLength].TrimEnd('-') : slug;
        }

        /// <summary>Short code for a category / sub-category: its first letters, e.g. "Laptops" -> "LAP".</summary>
        public static string CodeBase(string? name)
        {
            var letters = Slug(name, 100).Replace("-", string.Empty);
            if (letters.Length == 0) return "CAT";
            return letters.Length > 3 ? letters[..3] : letters;
        }

        /// <summary>SKU base: sub-category code + product name, e.g. "LAP-DELL-XPS-13".</summary>
        public static string SkuBase(string? subCategoryCode, string? productName)
        {
            var prefix = Slug(subCategoryCode, 15);
            var name = Slug(productName, SkuMaxLength - (prefix.Length > 0 ? prefix.Length + 1 : 0) - 4);
            if (name.Length == 0) name = "ITEM";
            return prefix.Length > 0 ? $"{prefix}-{name}" : name;
        }

        /// <summary>First candidate (the base, then base-2, base-3 ...) that is not taken yet.</summary>
        public static async Task<string> UniqueAsync(string baseValue, int maxLength, Func<string, Task<bool>> exists)
        {
            var candidate = baseValue.Length > maxLength ? baseValue[..maxLength] : baseValue;
            for (var n = 2; await exists(candidate); n++)
            {
                var suffix = $"-{n}";
                var stem = baseValue.Length > maxLength - suffix.Length ? baseValue[..(maxLength - suffix.Length)] : baseValue;
                candidate = stem + suffix;
            }
            return candidate;
        }
    }
}
