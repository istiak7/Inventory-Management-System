using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Queries.GetStockTransferById
{
    public sealed record GetStockTransferByIdQuery(int Id) : IRequest<Result>;
}
