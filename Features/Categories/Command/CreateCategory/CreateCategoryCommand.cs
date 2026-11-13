using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.CreateCategory
{
    public class CreateCategoryCommand : IRequest<Result>
    {
        public string CategoryName { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Code { get; set; }
    }
}
