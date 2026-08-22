namespace Inventory_Management_System.Features.Roles.Shared.Dtos
{
    public sealed record RoleResponse(
        int Id,
        string Name,
        string Description,
        int MemberCount,
        IReadOnlyList<int> PermissionIds,
        int PermissionCount
    );

    public sealed record PermissionResponse(
        int Id,
        string Name,
        string Description
    );
}
