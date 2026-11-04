using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.DeleteExpense
{
    public class DeleteExpenseCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
