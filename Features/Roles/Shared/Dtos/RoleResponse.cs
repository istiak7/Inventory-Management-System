namespace Inventory_Management_System.Features.Roles.Shared.Dtos
{
    public sealed record RoleResponse(
        int Id,
        string Name,
        string Description,
        int MemberCount,
        IReadOnlyList<int> PermissionIds,
        int PermissionCount,
        // Menu actions (System Setup -> Menu Action) the role has.
        IReadOnlyList<int> MenuActionIds,
        // The built-in Admin role: always has every menu and action, cannot be changed or removed.
        bool IsBuiltIn
    );

    public sealed record PermissionResponse(
        int Id,
        string Name,
        string Description
    );
}
