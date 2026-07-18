using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.CreateSubCategory
{
    public class CreateSubCategoryEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-sub-categories", async (CreateSubCategoryCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("SubCategory");
        }
    }
}
