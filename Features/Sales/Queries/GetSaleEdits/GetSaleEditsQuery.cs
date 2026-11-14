using System.Text.Json;
using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Sales.Queries.GetSaleEdits
{
    // The edit history of one sale, newest first: who changed it, why, the totals before and
    // after, and what moved (settlement, advance, refund, stock). Read by whoever may view Sales.
    public record GetSaleEditsQuery(int SaleId) : IRequest<Result>;

    public class GetSaleEditsHandler(AppDbContext _dbContext) : IRequestHandler<GetSaleEditsQuery, Result>
    {
        public async Task<Result> Handle(GetSaleEditsQuery request, CancellationToken cancellationToken)
        {
            // The sale itself is branch-scoped, so another branch's sale shows as not found.
            if (!await _dbContext.CustomerSales.AnyAsync(s => s.Id == request.SaleId, cancellationToken))
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Sale not found." };

            var rows = await _dbContext.SaleEdits
                .Where(e => e.SaleId == request.SaleId)
                .OrderByDescending(e => e.Id)
                .Select(e => new
                {
                    e.Id,
                    EditedAt = e.CreatedAt,
                    EditedBy = _dbContext.Users.Where(u => u.Id == e.CreatedById).Select(u => u.Name).FirstOrDefault(),
                    e.Reason,
                    e.OldSubTotal, e.OldDiscountAmount, e.OldTaxAmount, e.OldTotalAmount, e.OldPaidAmount, e.OldDueAmount,
                    e.NewSubTotal, e.NewDiscountAmount, e.NewTaxAmount, e.NewTotalAmount, e.NewPaidAmount, e.NewDueAmount,
                    e.ReturnedAmount, e.SettlementReversed, e.SettlementRedone,
                    e.CashReleased, e.CashRefunded, e.CashReapplied, e.AdditionalPayment,
                    e.ChangesJson,
                })
                .ToListAsync(cancellationToken);

            var data = rows.Select(e => new
            {
                e.Id, e.EditedAt, e.EditedBy, e.Reason,
                e.OldSubTotal, e.OldDiscountAmount, e.OldTaxAmount, e.OldTotalAmount, e.OldPaidAmount, e.OldDueAmount,
                e.NewSubTotal, e.NewDiscountAmount, e.NewTaxAmount, e.NewTotalAmount, e.NewPaidAmount, e.NewDueAmount,
                e.ReturnedAmount, e.SettlementReversed, e.SettlementRedone,
                e.CashReleased, e.CashRefunded, e.CashReapplied, e.AdditionalPayment,
                Changes = JsonSerializer.Deserialize<JsonElement>(e.ChangesJson),
            }).ToList();

            return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Edit history retrieved", Data = data };
        }
    }

    public class GetSaleEditsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-sale-edits/{saleId:int}", async (int saleId, IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetSaleEditsQuery(saleId))))
                .WithTags("Sales").RequirePermission(Permissions.SalesView);
        }
    }
}
