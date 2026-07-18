using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetProductById
{
    public sealed record GetProductByIdQuery(int Id) : IRequest<Result>;
}
