using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.GetWarrantyClaimById
{
    public sealed record GetWarrantyClaimByIdQuery(int Id) : IRequest<Result>;
}
