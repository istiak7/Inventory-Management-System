using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.DeleteCategory
{
    public class DeleteCategoryCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
