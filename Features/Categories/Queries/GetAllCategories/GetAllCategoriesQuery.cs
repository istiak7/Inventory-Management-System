using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetAllCategories
{
    public sealed record GetAllCategoriesQuery(int PageNumber = 1, int PageSize = 20) : IRequest<Result>;
}
