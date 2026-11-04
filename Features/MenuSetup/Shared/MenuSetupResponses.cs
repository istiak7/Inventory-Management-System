namespace Inventory_Management_System.Features.MenuSetup.Shared
{
    public sealed record ModuleResponse(int Id, string Name, string Icon, int DisplayOrder, int SubmoduleCount);

    public sealed record SubmoduleResponse(
        int Id, int ModuleId, string ModuleName, string Name, int DisplayOrder, int MenuCount);

    public sealed record MenuResponse(
        int Id,
        int SubmoduleId,
        int? ParentId,
        string Name,
        string FrontendRoute,
        int MenuOrder,
        bool IsLeaf,
        string Icon,
        string Permission,
        int ChildCount,
        int ActionCount);

    public sealed record MenuActionResponse(
        int Id, int MenuId, string Name, string ComponentCode, IReadOnlyList<string> ApiRoutes);

    // The navigation tree the app's menus are built from.
    public sealed record NavigationMenu(
        int Id,
        string Name,
        string Route,
        string Icon,
        string Permission,
        bool IsLeaf,
        IReadOnlyList<NavigationMenu> Children);

    public sealed record NavigationSubmodule(int Id, string Name, IReadOnlyList<NavigationMenu> Menus);

    public sealed record NavigationModule(
        int Id, string Name, string Icon, IReadOnlyList<NavigationSubmodule> Submodules);
}
