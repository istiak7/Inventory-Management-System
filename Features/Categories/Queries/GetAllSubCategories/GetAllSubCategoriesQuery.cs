using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetAllSubCategories
{
    public sealed record GetAllSubCategoriesQuery(int PageNumber = 1, int PageSize = 20) : IRequest<Result>;
}
