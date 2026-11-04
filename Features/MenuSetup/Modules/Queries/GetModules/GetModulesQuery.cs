using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Modules.Queries.GetModules
{
    public sealed record GetModulesQuery : IRequest<Result>;
}
