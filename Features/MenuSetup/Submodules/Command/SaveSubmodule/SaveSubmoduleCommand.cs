using Inventory_Management_System.Shared;
using MediatR;
using System.Text.Json.Serialization;

namespace Inventory_Management_System.Features.MenuSetup.Submodules.Command.SaveSubmodule
{
    public class SaveSubmoduleCommand : IRequest<Result>
    {
        [JsonIgnore] public int? Id { get; set; }
        public int ModuleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
