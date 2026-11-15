using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Command.DeleteUnitOfMeasure
{
    public class DeleteUnitOfMeasureCommand : IRequest<Result>
    {
        public int Id { get; set; }
    }
}
