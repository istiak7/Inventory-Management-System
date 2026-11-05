using Inventory_Management_System.Shared;
using MediatR;
using System.Text.Json.Serialization;

namespace Inventory_Management_System.Features.Sales.Command.VoidSale
{
    public class VoidSaleCommand : IRequest<Result>
    {
        [JsonIgnore] public int Id { get; set; }

        // Why the sale is being cancelled; kept on the sale for audit.
        public string Reason { get; set; } = string.Empty;
    }
}
