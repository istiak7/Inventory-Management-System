using MediatR;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Activities.CreateActivity
{
    public sealed record CreateActivityCommand(
        string Title,
        string Description) : IRequest<Result>;
}
