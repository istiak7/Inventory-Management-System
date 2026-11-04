using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.MenuActions.Command.DeleteMenuAction
{
    public sealed record DeleteMenuActionCommand(int Id) : IRequest<Result>;
}
