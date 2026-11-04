using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.SystemSetup.Command.SaveCompanySetting
{
    // Creates the company row the first time, updates it after that.
    public class SaveCompanySettingCommand : IRequest<Result>
    {
        public string Name { get; set; } = string.Empty;
        public string? Tagline { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string? QrUrl { get; set; }
        public string? InvoiceFooter { get; set; }
    }
}
