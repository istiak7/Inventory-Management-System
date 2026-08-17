using Inventory_Management_System.Features.Customers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Command.CreateCustomerPayment
{
    public class CreateCustomerPaymentEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-customer-payment", async (CreateCustomerPaymentRequest request, IMediator mediator) =>
            {
                var command = new CreateCustomerPaymentCommand
                {
                    CustomerId = request.CustomerId,
                    BranchId = request.BranchId,
                    Amount = request.Amount,
                    PaymentDate = request.PaymentDate,
                    PaymentMethod = request.PaymentMethod,
                    Remarks = request.Remarks,
                    Allocations = request.Allocations ?? [],
                };
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Customer");
        }
    }
}
