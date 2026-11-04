using Inventory_Management_System.Entities;

namespace Inventory_Management_System.Features.SystemSetup.Shared.Dtos
{
    public sealed record CompanySettingResponse(
        string Name,
        string Tagline,
        string Address,
        string Phone,
        string Email,
        string Website,
        string LogoUrl,
        string QrUrl,
        string InvoiceFooter,
        DateTime UpdatedAt)
    {
        public static CompanySettingResponse From(CompanySetting c) => new(
            c.Name, c.Tagline, c.Address, c.Phone, c.Email, c.Website, c.LogoUrl, c.QrUrl, c.InvoiceFooter,
            c.UpDatedAt == default ? c.CreatedAt : c.UpDatedAt);
    }
}
