using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Queries.GetExpenseCategories.GetAllExpenseCategories
{
    public class GetAllExpenseCategoriesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/office-expense/categories", async (IMediator mediator, int pageNumber = 1, int pageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllExpenseCategoriesQuery(pageNumber, pageSize));
                return Results.Ok(result);
            })
            .WithName("GetAllExpenseCategories")
            .WithTags("OfficeExpense");
        }
    }
}
