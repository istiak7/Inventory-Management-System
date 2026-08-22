namespace Inventory_Management_System.Features.Users.Shared.Dtos
{
    // One row in the users list.
    public sealed record UserResponse(
        int Id,
        string Name,
        string Email,
        int RoleId,
        string RoleName,
        int? BranchId,
        string? BranchName,
        int IsActive,
        DateTime CreatedAt
    );

    // Full user with the permissions that actually apply to them
    // (role permissions + the extra ones granted directly).
    public sealed record UserDetailResponse(
        int Id,
        string Name,
        string Email,
        int RoleId,
        string RoleName,
        int? BranchId,
        string? BranchName,
        int IsActive,
        DateTime CreatedAt,
        IReadOnlyList<PermissionResponse> Permissions,
        IReadOnlyList<int> DirectPermissionIds
    );

    public sealed record PermissionResponse(
        int Id,
        string Name,
        string Description
    );
}
