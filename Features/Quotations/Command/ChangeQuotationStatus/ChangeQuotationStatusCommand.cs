using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Command.ChangeQuotationStatus
{
    public class ChangeQuotationStatusCommand : IRequest<Result>
    {
        // Taken from the route, never from the body.
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
