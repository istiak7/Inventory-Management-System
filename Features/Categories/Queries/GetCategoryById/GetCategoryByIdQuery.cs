using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetCategoryById
{
    public sealed record GetCategoryByIdQuery(int Id) : IRequest<Result>;
}
