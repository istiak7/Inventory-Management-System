using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Command.CreateSupplierPayment
{
    public class CreateSupplierPaymentEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-supplier-payment", async (CreateSupplierPaymentRequest request, IMediator mediator) =>
            {
                var command = new CreateSupplierPaymentCommand
                {
                    SupplierId = request.SupplierId,
                    BranchId = request.BranchId,
                    Amount = request.Amount,
                    PaymentDate = request.PaymentDate,
                    PaymentMethod = request.PaymentMethod,
                    Remarks = request.Remarks,
                    Allocations = request.Allocations ?? [],
                };
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Supplier").RequirePermission(Permissions.SuppliersManage);
        }
    }
}
