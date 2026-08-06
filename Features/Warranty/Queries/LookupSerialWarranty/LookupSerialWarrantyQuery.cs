using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.LookupSerialWarranty
{
    public sealed record LookupSerialWarrantyQuery(string SerialNumber) : IRequest<Result>;
}
