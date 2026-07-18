using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.UpdateCategory
{
    public class UpdateCategoryCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Code { get; set; }
    }
}
