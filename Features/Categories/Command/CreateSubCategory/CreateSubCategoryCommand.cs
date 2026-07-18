using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.CreateSubCategory
{
    public class CreateSubCategoryCommand : IRequest<Result>
    {
        public string SubCategoryName { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Code { get; set; }
        public int ProductCategoryId { get; set; }
    }
}
