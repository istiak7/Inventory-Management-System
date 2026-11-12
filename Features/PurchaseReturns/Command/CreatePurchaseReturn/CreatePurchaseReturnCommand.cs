using FluentValidation;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.PurchaseReturns.Command.CreatePurchaseReturn
{
    public class CreatePurchaseReturnCommand : IRequest<Result>
    {
        public int PurchaseId { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public List<PurchaseReturnLineRequest> Lines { get; set; } = [];
    }

    public class PurchaseReturnLineRequest
    {
        public int SupplierPurchaseDetailsId { get; set; }
        public int Quantity { get; set; }
        // Serialized lines: exactly Quantity serial numbers from this line that are in stock.
        public List<string>? SerialNumbers { get; set; }
    }

    public class CreatePurchaseReturnValidator : AbstractValidator<CreatePurchaseReturnCommand>
    {
        public CreatePurchaseReturnValidator()
        {
            RuleFor(x => x.PurchaseId).GreaterThan(0).WithMessage("Choose the purchase bill to return from.");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Give a reason for the return.")
                .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
            RuleFor(x => x.Remarks).MaximumLength(500).WithMessage("Remarks must be 500 characters or fewer.");
            RuleFor(x => x.Lines).NotEmpty().WithMessage("Choose at least one product to return.");
            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.SupplierPurchaseDetailsId).GreaterThan(0).WithMessage("Each returned line must point to a bill line.");
                line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Return quantity must be more than 0.");
            });
            RuleFor(x => x.Lines)
                .Must(lines => lines.Select(l => l.SupplierPurchaseDetailsId).Distinct().Count() == lines.Count)
                .WithMessage("The same product line is listed twice.");
        }
    }

    public class CreatePurchaseReturnEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-purchase-return", async (CreatePurchaseReturnCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Purchase Return").RequirePermission(Permissions.PurchasesManage);
        }
    }
}
