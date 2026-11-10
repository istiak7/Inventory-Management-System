using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Command.DeleteQuotation
{
    public sealed record DeleteQuotationCommand(int Id) : IRequest<Result>;
}
