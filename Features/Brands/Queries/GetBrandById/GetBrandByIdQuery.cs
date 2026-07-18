using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Queries.GetBrandById
{
    public sealed record GetBrandByIdQuery(int Id) : IRequest<Result>;
}
