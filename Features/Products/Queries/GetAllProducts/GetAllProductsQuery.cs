using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetAllProducts
{
    public sealed record GetAllProductsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<Result>;
}
