using Inventory_Management_System.Shared;
using MediatR;
using System.Text.Json.Serialization;

namespace Inventory_Management_System.Features.MenuSetup.Modules.Command.SaveModule
{
    // Create (Id = null) or update a module. The id comes from the route on update.
    public class SaveModuleCommand : IRequest<Result>
    {
        [JsonIgnore] public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
    }
}
