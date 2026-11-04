using Inventory_Management_System.Database;
using Inventory_Management_System.Features.SystemSetup.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.SystemSetup.Queries.GetCompanySetting
{
    public class GetCompanySettingQueryHandler(AppDbContext _db) : IRequestHandler<GetCompanySettingQuery, Result>
    {
        public async Task<Result> Handle(GetCompanySettingQuery request, CancellationToken cancellationToken)
        {
            var company = await _db.CompanySettings.AsNoTracking()
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            // Not saved yet is a normal state, not an error: Data is null and the app keeps
            // printing its built-in letterhead until an admin fills in Company Setup.
            return new Result
            {
                IsSuccess = true,
                StatusCode = 200,
                Status = "Success",
                Message = company is null ? "Company information has not been set up yet." : "Company information retrieved successfully",
                Data = company is null ? null! : CompanySettingResponse.From(company)
            };
        }
    }
}
