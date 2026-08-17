using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerAccounts
{
    public sealed record GetCustomerAccountsQuery(int? CustomerId = null) : IRequest<Result>;
}
