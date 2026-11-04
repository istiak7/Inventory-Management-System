using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.MenuActions.Queries.GetMenuActions
{
    public sealed record GetMenuActionsQuery(int MenuId) : IRequest<Result>;
}
