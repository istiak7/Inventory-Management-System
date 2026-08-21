using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetProductVariants
{
    public sealed record GetProductVariantsQuery(int? ProductId = null) : IRequest<Result>;
}
