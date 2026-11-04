using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Menus.Command.DeleteMenu
{
    public sealed record DeleteMenuCommand(int Id) : IRequest<Result>;
}
