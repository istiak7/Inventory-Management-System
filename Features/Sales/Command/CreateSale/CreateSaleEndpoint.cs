using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Command.CreateSale
{
    public class CreateSaleEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-sale", async (CreateSaleRequest request, IMediator mediator) =>
            {
                var command = new CreateSaleCommand
                {
                    CustomerId = request.CustomerId,
                    Customer = request.Customer,
                    BranchId = request.BranchId,
                    SaleDate = request.SaleDate,
                    InvoiceNumber = request.InvoiceNumber,
                    Remarks = request.Remarks,
                    Items = request.Items,
                    DiscountAmount = request.DiscountAmount,
                    TaxAmount = request.TaxAmount,
                    Payment = request.Payment
                };
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Sales").RequirePermission(Permissions.SalesManage);
        }
    }
}
