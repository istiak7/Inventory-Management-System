using Inventory_Management_System.Features.Quotations.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Command.UpdateQuotation
{
    public class UpdateQuotationCommand : SaveQuotationRequest, IRequest<Result>
    {
        // Taken from the route, never from the body.
        public int Id { get; set; }
    }
}
