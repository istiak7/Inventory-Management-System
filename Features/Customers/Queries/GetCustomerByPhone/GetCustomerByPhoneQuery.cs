using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerByPhone
{
    /// <summary>
    /// Exact-identity lookup used by the sales form before it offers to create a customer.
    /// Distinct from <see cref="GetAllCustomers.GetAllCustomersQuery"/>'s <c>Search</c>, which is a
    /// fuzzy ILIKE over name and phone for browsing — this one answers "is this exact number
    /// already on file?" and nothing else.
    /// </summary>
    public sealed record GetCustomerByPhoneQuery(string PhoneNumber) : IRequest<Result>;
}
