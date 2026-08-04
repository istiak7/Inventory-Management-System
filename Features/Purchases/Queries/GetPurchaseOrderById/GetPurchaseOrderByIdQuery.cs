using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Queries.GetPurchaseOrderById
{
    public sealed record GetPurchaseOrderByIdQuery(int Id) : IRequest<Result>;
}
