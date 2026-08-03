using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Command.RejectStockTransfer
{
    public class RejectStockTransferCommand : IRequest<Result>
    {
        public int StockTransferId { get; set; }
    }
}
