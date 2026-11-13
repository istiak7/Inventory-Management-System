using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.CreateSubCategory
{
    public class CreateSubCategoryCommand : IRequest<Result>
    {
        public string SubCategoryName { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Code { get; set; }
        public int ProductCategoryId { get; set; }
    }
}
