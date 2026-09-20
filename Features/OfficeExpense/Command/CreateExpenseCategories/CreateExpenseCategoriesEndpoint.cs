using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.CreateExpenseCategories
{
    public class CreateExpenseCategoriesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/office-expense/categories", async (CreateExpenseCategoriesCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            })
            .WithName("CreateExpenseCategories")
            .WithTags("OfficeExpense");

        }
    }
}
