using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.DeleteSubCategory
{
    public class DeleteSubCategoryCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
