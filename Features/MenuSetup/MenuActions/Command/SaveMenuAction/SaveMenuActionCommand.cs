using Inventory_Management_System.Shared;
using MediatR;
using System.Text.Json.Serialization;

namespace Inventory_Management_System.Features.MenuSetup.MenuActions.Command.SaveMenuAction
{
    public class SaveMenuActionCommand : IRequest<Result>
    {
        [JsonIgnore] public int? Id { get; set; }
        public int MenuId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ComponentCode { get; set; } = string.Empty;
        // "METHOD /route" entries from /get-api-routes.
        public List<string> ApiRoutes { get; set; } = [];
    }
}
