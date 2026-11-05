using Inventory_Management_System.Features.Transfers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.CurrentUser;
using MediatR;
using System.Text.Json.Serialization;

namespace Inventory_Management_System.Features.Transfers.Command.UpdateStockTransfer
{
    // Same body as create. Replaces the transfer's branches, lines and notes while it is still a
    // draft or pending (nothing has moved yet). Dispatch = true sends it (Pending), false keeps
    // it as a draft.
    public class UpdateStockTransferCommand : IRequest<Result>, IBranchScopedRequest
    {
        public bool IsAllowedForBranch(int userBranchId) => SourceBranchId == userBranchId || DestinationBranchId == userBranchId;

        [JsonIgnore] public int Id { get; set; }
        public int SourceBranchId { get; set; }
        public int DestinationBranchId { get; set; }
        public List<TransferItemRequest> Items { get; set; } = [];
        public string? Notes { get; set; }
        public bool Dispatch { get; set; }
    }
}
