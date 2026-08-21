using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetAllCustomers
{
    public sealed record GetAllCustomersQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? Search = null
    ) : IRequest<Result>;
}
