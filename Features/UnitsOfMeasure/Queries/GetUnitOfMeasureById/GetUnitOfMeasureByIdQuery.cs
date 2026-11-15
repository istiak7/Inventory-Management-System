using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Queries.GetUnitOfMeasureById
{
    public sealed record GetUnitOfMeasureByIdQuery(int Id) : IRequest<Result>;
}
