using FluentValidation;
using Inventory_Management_System.Features.DataUpload.Shared;

namespace Inventory_Management_System.Features.DataUpload.Command.ProcessDataUpload
{
    public class ProcessDataUploadValidator : AbstractValidator<ProcessDataUploadCommand>
    {
        public ProcessDataUploadValidator()
        {
            RuleFor(x => x.Type)
                .Must(type => DataUploadCatalog.Find(type) is not null)
                .WithMessage("Please select a valid Excel type.");

            RuleFor(x => x.File)
                .Must(file => UploadFileReader.CheckFile(file) is null)
                .WithMessage(x => UploadFileReader.CheckFile(x.File) ?? string.Empty);
        }
    }
}
