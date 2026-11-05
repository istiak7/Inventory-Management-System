using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Command.DeleteStockTransfer
{
    public class DeleteStockTransferCommand : IRequest<Result>
    {
        public int StockTransferId { get; set; }
    }
}
