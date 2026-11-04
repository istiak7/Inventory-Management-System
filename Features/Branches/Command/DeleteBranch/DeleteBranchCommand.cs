using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Command.DeleteBranch
{
    public class DeleteBranchCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
