using Inventory_Management_System.Features.Sales.Command.CreateSale;
using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Command.ConvertQuotationToSale
{
    // Turns an Accepted quotation into a sale. The body is the same as create-sale (branch,
    // serials, payment), and the sale is made by the normal CreateSaleHandler, so stock, serials,
    // the customer ledger and payments work exactly as for any sale. The handler also checks the
    // quotation and links it to the new sale in the same transaction.
    //
    // It has its own route (not a flag on create-sale) so System Setup can give "Convert to
    // Invoice" on the Price Quotation page separately from "Create" on the Sales page.
    public class ConvertQuotationToSaleEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/convert-quotation-to-sale/{id:int}", async (int id, CreateSaleRequest request, IMediator mediator) =>
            {
                var command = new CreateSaleCommand
                {
                    QuotationId = id,
                    CustomerId = request.CustomerId,
                    Customer = request.Customer,
                    BranchId = request.BranchId,
                    SaleDate = request.SaleDate,
                    InvoiceNumber = request.InvoiceNumber,
                    Remarks = request.Remarks,
                    Items = request.Items,
                    DiscountAmount = request.DiscountAmount,
                    TaxAmount = request.TaxAmount,
                    PaymentType = request.PaymentType,
                    Payment = request.Payment
                };
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Quotations").RequirePermission(Permissions.SalesManage);
        }
    }
}
