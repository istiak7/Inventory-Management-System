using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.DeleteExpense
{
    public class DeleteExpenseEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-office-expense/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteExpenseCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("OfficeExpense").RequireAuthorization();
        }
    }
}
