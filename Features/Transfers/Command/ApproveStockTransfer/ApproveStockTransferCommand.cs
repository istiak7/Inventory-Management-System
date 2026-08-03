using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Command.ApproveStockTransfer
{
    public class ApproveStockTransferCommand : IRequest<Result>
    {
        public int StockTransferId { get; set; }
    }
}
