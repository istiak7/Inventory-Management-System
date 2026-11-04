using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.SystemSetup.Queries.GetSystemSetupStatus
{
    // Read-only: checks every setup section in one call. Each section is still saved on its own screen.
    public sealed record GetSystemSetupStatusQuery : IRequest<Result>;
}
