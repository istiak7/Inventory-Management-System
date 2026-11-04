using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.SystemSetup.Queries.GetCompanySetting
{
    public sealed record GetCompanySettingQuery : IRequest<Result>;
}
