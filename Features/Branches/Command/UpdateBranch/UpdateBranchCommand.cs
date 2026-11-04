using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Command.UpdateBranch
{
    public class UpdateBranchCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
