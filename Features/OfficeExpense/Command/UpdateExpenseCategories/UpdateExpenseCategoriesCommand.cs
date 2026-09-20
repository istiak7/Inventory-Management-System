using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.UpdateExpenseCategories
{
    public class UpdateExpenseCategoriesCommand : IRequest<Result>
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
