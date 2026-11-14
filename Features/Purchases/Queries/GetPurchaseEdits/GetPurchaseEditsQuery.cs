using System.Text.Json;
using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Purchases.Queries.GetPurchaseEdits
{
    // The edit history of one purchase, newest first: who changed it, why, the totals before and
    // after, and what moved (settlement, advance, refund, stock). Read by whoever may view Purchases.
    public record GetPurchaseEditsQuery(int PurchaseId) : IRequest<Result>;

    public class GetPurchaseEditsHandler(AppDbContext _dbContext) : IRequestHandler<GetPurchaseEditsQuery, Result>
    {
        public async Task<Result> Handle(GetPurchaseEditsQuery request, CancellationToken cancellationToken)
        {
            // The purchase itself is branch-scoped, so another branch's bill shows as not found.
            if (!await _dbContext.SupplierPurchases.AnyAsync(p => p.Id == request.PurchaseId, cancellationToken))
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Purchase order not found." };

            var rows = await _dbContext.PurchaseEdits
                .Where(e => e.PurchaseId == request.PurchaseId)
                .OrderByDescending(e => e.Id)
                .Select(e => new
                {
                    e.Id,
                    EditedAt = e.CreatedAt,
                    EditedBy = _dbContext.Users.Where(u => u.Id == e.CreatedById).Select(u => u.Name).FirstOrDefault(),
                    e.Reason,
                    e.OldDiscountAmount, e.OldTotalAmount, e.OldPaidAmount, e.OldDueAmount,
                    e.NewDiscountAmount, e.NewTotalAmount, e.NewPaidAmount, e.NewDueAmount,
                    e.ReturnedAmount, e.SettlementReversed, e.SettlementRedone,
                    e.CashReleased, e.CashRefunded, e.CashReapplied, e.AdditionalPayment,
                    e.ChangesJson,
                })
                .ToListAsync(cancellationToken);

            var data = rows.Select(e => new
            {
                e.Id, e.EditedAt, e.EditedBy, e.Reason,
                e.OldDiscountAmount, e.OldTotalAmount, e.OldPaidAmount, e.OldDueAmount,
                e.NewDiscountAmount, e.NewTotalAmount, e.NewPaidAmount, e.NewDueAmount,
                e.ReturnedAmount, e.SettlementReversed, e.SettlementRedone,
                e.CashReleased, e.CashRefunded, e.CashReapplied, e.AdditionalPayment,
                Changes = JsonSerializer.Deserialize<JsonElement>(e.ChangesJson),
            }).ToList();

            return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Edit history retrieved", Data = data };
        }
    }

    public class GetPurchaseEditsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-purchase-edits/{purchaseId:int}", async (int purchaseId, IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetPurchaseEditsQuery(purchaseId))))
                .WithTags("Purchases").RequirePermission(Permissions.PurchasesView);
        }
    }
}
