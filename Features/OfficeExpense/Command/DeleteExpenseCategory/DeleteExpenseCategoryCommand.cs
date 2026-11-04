using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.DeleteExpenseCategory
{
    public class DeleteExpenseCategoryCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
