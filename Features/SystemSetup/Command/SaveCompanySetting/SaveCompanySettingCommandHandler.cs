using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.SystemSetup.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.SystemSetup.Command.SaveCompanySetting
{
    public class SaveCompanySettingCommandHandler(
        AppDbContext _db,
        ILogger<SaveCompanySettingCommandHandler> _logger
    ) : IRequestHandler<SaveCompanySettingCommand, Result>
    {
        public async Task<Result> Handle(SaveCompanySettingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // One company per installation: update the existing row, or create it the first time.
                var company = await _db.CompanySettings.OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);
                var isNew = company is null;

                company ??= new CompanySetting { Name = string.Empty, CreatedAt = DateTime.UtcNow };
                company.Name = request.Name.Trim();
                company.Tagline = Clean(request.Tagline);
                company.Address = NormalizeLines(request.Address);
                company.Phone = request.Phone.Trim();
                company.Email = Clean(request.Email);
                company.Website = Clean(request.Website);
                company.LogoUrl = Clean(request.LogoUrl);
                company.QrUrl = Clean(request.QrUrl);
                company.InvoiceFooter = Clean(request.InvoiceFooter);

                if (isNew)
                    _db.CompanySettings.Add(company);
                else
                    company.UpDatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = isNew ? 201 : 200,
                    Status = "Success",
                    Message = "Company information saved successfully",
                    Data = CompanySettingResponse.From(company)
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error saving company information");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while saving the company information."
                };
            }
        }

        private static string Clean(string? value) => value?.Trim() ?? string.Empty;

        // Trim every line and drop empty ones, so the letterhead never prints blank lines.
        private static string NormalizeLines(string value) =>
            string.Join('\n', value.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    }
}
