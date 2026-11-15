using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Command.CreateUnitOfMeasure
{
    public class CreateUnitOfMeasureCommand : IRequest<Result>
    {
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
