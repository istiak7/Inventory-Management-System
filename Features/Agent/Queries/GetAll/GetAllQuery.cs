using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Agent.Queries.GetAll
{
    public sealed record GetAllQuery(string searchTerm = "") : IRequest<Result>;
}
