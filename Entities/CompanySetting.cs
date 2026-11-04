namespace Inventory_Management_System.Entities
{
    // The shop's own identity, printed on every invoice and receipt.
    // There is only ever one row (see SaveCompanySettingCommandHandler).
    public class CompanySetting : BaseEntity
    {
        public required string Name { get; set; }
        public string Tagline { get; set; } = string.Empty;

        // Printed line by line on the letterhead (newline-separated).
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;

        // Image paths/URLs, e.g. "/assets/images/company-logo.png".
        public string LogoUrl { get; set; } = string.Empty;
        public string QrUrl { get; set; } = string.Empty;

        // Small print at the bottom of invoices, e.g. return policy.
        public string InvoiceFooter { get; set; } = string.Empty;
    }
}
