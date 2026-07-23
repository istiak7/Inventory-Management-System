namespace Inventory_Management_System.Features.Branches.Shared.Dtos
{
    public sealed record BranchResponse(
        int Id,
        string Name,
        string Location,
        string PhoneNumber,
        string Email
    );
}
