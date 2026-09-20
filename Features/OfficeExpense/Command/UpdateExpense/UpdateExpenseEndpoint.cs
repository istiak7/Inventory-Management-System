using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.UpdateExpense
{
    public class UpdateExpenseEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/office-expense/update-expense", async (UpdateExpenseCommand request, IMediator mediator) =>
            {
                var result = await mediator.Send(request);
                return Results.Ok(result);
            })
            .WithName("UpdateExpense")
            .WithTags("OfficeExpense");
        }
    }
}
