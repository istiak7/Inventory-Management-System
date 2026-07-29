using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Queries.GetSaleById
{
    public sealed record GetSaleByIdQuery(int Id) : IRequest<Result>;
}
