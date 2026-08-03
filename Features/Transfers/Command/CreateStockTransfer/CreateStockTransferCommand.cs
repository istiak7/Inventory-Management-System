using Inventory_Management_System.Features.Transfers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Command.CreateStockTransfer
{
    public class CreateStockTransferCommand : IRequest<Result>
    {
        public int SourceBranchId { get; set; }
        public int DestinationBranchId { get; set; }
        public List<TransferItemRequest> Items { get; set; } = [];
        public string? Notes { get; set; }
        public bool Dispatch { get; set; }
    }
}
