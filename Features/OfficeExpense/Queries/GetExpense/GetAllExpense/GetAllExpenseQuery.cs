using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Queries.GetExpense.GetAllExpense
{
    public sealed record GetAllExpenseQuery(int pageNumber = 1, int pageSize = 10) : IRequest<Result>;
}
