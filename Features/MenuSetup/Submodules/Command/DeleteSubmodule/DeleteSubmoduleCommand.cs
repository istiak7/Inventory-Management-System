using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Submodules.Command.DeleteSubmodule
{
    public sealed record DeleteSubmoduleCommand(int Id) : IRequest<Result>;
}
