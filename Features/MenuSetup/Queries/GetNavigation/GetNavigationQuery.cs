using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Queries.GetNavigation
{
    // The whole menu tree the app's top bar, sidebar and launcher are built from.
    public sealed record GetNavigationQuery : IRequest<Result>;
}
