using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Command.UpdateSaleRemarks
{
    public class UpdateSaleRemarksCommand : IRequest<Result>
    {
        public int Id { get; set; }

        public string? Remarks { get; set; }
    }
}
