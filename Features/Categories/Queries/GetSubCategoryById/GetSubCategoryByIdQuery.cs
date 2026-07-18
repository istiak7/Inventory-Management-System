using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetSubCategoryById
{
    public sealed record GetSubCategoryByIdQuery(int Id) : IRequest<Result>;
}
