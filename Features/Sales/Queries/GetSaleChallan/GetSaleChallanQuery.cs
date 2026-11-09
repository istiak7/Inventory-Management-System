using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Queries.GetSaleChallan
{
    public sealed record GetSaleChallanQuery(int SaleId) : IRequest<Result>;
}
