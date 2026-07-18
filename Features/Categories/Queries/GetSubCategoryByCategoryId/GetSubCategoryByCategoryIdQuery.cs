using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetSubCategoryByCategoryId
{
    public sealed record GetSubCategoryByCategoryIdQuery(int CategoryId, int PageNumber = 1, int PageSize = 20)
        : IRequest<Result>;
}
