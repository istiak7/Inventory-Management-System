using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Menus.Queries.GetMenus
{
    // Every menu of one submodule as a flat list; the page builds the tree from ParentId.
    public sealed record GetMenusQuery(int SubmoduleId) : IRequest<Result>;
}
