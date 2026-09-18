using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.CreateExpenseCategories
{
    public class CreateExpenseCategoriesCommand : IRequest<Result>
    {
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
