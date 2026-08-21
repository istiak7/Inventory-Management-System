using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerByPhone
{
    public sealed record GetCustomerByPhoneQuery(string PhoneNumber) : IRequest<Result>;
}
