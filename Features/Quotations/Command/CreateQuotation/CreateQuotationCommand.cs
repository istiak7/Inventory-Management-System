using Inventory_Management_System.Features.Quotations.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Command.CreateQuotation
{
    public class CreateQuotationCommand : SaveQuotationRequest, IRequest<Result>
    {
    }
}
