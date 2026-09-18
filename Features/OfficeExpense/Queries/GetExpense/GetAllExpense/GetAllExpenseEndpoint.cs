using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Queries.GetExpense.GetAllExpense
{
    public class GetAllExpenseEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/office-expense", async (IMediator mediator, int pageNumber = 1, int pageSize = 10) =>
            {
                var query = new GetAllExpenseQuery(pageNumber, pageSize);
                var result = await mediator.Send(query);
                return Results.Ok(result);
            })
            .WithName("GetAllExpense")
            .WithTags("OfficeExpense");
        }
    }
}
