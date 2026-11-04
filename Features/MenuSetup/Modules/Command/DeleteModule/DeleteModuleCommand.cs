using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Modules.Command.DeleteModule
{
    public sealed record DeleteModuleCommand(int Id) : IRequest<Result>;
}
