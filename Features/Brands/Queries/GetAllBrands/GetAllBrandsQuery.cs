using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Queries.GetAllBrands
{
    public sealed record GetAllBrandsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<Result>;
}
