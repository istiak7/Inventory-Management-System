using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Queries.GetProductsByBrandId
{
    public sealed record GetProductsByBrandIdQuery(int BrandId, int PageNumber = 1, int PageSize = 20)
        : IRequest<Result>;
}
