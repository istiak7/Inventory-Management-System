using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetProductVariants
{
    // Lists variants, optionally scoped to one product. Variants per product are few,
    // so this returns a plain list (no pagination).
    public sealed record GetProductVariantsQuery(int? ProductId = null) : IRequest<Result>;
}
