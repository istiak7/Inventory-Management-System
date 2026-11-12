using FluentValidation;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.SalesReturns.Command.CancelSalesReturn
{
    public class CancelSalesReturnCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class CancelSalesReturnValidator : AbstractValidator<CancelSalesReturnCommand>
    {
        public CancelSalesReturnValidator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Give a reason for cancelling the return.")
                .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
        }
    }

    public class CancelSalesReturnEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/cancel-sales-return/{id:int}", async (int id, CancelSalesReturnCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Sales Return").RequirePermission(Permissions.SalesManage);
        }
    }
}
