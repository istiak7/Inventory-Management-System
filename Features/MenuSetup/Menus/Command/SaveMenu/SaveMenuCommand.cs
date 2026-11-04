using Inventory_Management_System.Shared;
using MediatR;
using System.Text.Json.Serialization;

namespace Inventory_Management_System.Features.MenuSetup.Menus.Command.SaveMenu
{
    public class SaveMenuCommand : IRequest<Result>
    {
        [JsonIgnore] public int? Id { get; set; }
        public int SubmoduleId { get; set; }
        // The folder this menu sits in ("Move To"). Null = a top-level menu.
        public int? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? FrontendRoute { get; set; }
        public int MenuOrder { get; set; }
        // True = a page that opens FrontendRoute; false = a folder of other menus.
        public bool IsLeaf { get; set; }
        public string? Icon { get; set; }
        public string? Permission { get; set; }
    }
}
