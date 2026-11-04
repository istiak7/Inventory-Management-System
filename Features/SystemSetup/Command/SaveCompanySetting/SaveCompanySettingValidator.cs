using FluentValidation;

namespace Inventory_Management_System.Features.SystemSetup.Command.SaveCompanySetting
{
    public class SaveCompanySettingValidator : AbstractValidator<SaveCompanySettingCommand>
    {
        public SaveCompanySettingValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Company name is required.")
                .MaximumLength(150).WithMessage("Company name must be 150 characters or fewer.");
            RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.")
                .MaximumLength(500).WithMessage("Address must be 500 characters or fewer.");
            RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone is required.")
                .MaximumLength(100).WithMessage("Phone must be 100 characters or fewer.");

            RuleFor(x => x.Email).EmailAddress().WithMessage("A valid email is required.")
                .MaximumLength(150).WithMessage("Email must be 150 characters or fewer.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Tagline).MaximumLength(200).WithMessage("Tagline must be 200 characters or fewer.");
            RuleFor(x => x.Website).MaximumLength(200).WithMessage("Website must be 200 characters or fewer.");
            RuleFor(x => x.LogoUrl).MaximumLength(500).WithMessage("Logo URL must be 500 characters or fewer.");
            RuleFor(x => x.QrUrl).MaximumLength(500).WithMessage("QR code URL must be 500 characters or fewer.");
            RuleFor(x => x.InvoiceFooter).MaximumLength(500).WithMessage("Invoice footer must be 500 characters or fewer.");
        }
    }
}
