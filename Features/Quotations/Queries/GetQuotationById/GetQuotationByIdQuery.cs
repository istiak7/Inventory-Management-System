using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Quotations.Queries.GetQuotationById
{
    public sealed record GetQuotationByIdQuery(int Id) : IRequest<Result>;
}
