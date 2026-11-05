using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.DeleteWarrantyClaim
{
    public class DeleteWarrantyClaimCommand : IRequest<Result>
    {
        public int WarrantyClaimId { get; set; }
    }
}
