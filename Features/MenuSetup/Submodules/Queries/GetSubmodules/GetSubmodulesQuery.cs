using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Submodules.Queries.GetSubmodules
{
    public sealed record GetSubmodulesQuery : IRequest<Result>;
}
