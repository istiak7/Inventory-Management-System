using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.DeleteExpenseCategory
{
    public class DeleteExpenseCategoryEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-expense-categories/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteExpenseCategoryCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("OfficeExpense").RequireAuthorization();
        }
    }
}
