using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerPayments
{
    public sealed record GetCustomerPaymentsQuery(int CustomerId) : IRequest<Result>;
}
