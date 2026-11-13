using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.UpdateSubCategory
{
    public class UpdateSubCategoryCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public string SubCategoryName { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Code { get; set; }
        public int ProductCategoryId { get; set; }
    }
}
