using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Queries.GetMenuActionTree
{
    public sealed record GetMenuActionTreeQuery : IRequest<Result>;

    // Every page with its actions, grouped by module: what the role editor ticks.
    public sealed record MenuActionTreeAction(int Id, string Name, string ComponentCode);

    public sealed record MenuActionTreePage(
        int MenuId,
        string Name,
        string Route,
        // Folder names above the page, e.g. ["Operations", "Sales"].
        IReadOnlyList<string> Path,
        IReadOnlyList<MenuActionTreeAction> Actions);

    public sealed record MenuActionTreeModule(int Id, string Name, IReadOnlyList<MenuActionTreePage> Pages);
}
