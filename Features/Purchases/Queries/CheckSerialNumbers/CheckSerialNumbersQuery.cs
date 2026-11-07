using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Queries.CheckSerialNumbers
{
    // Which of these serial numbers are already used (in stock, sold, or entered on another
    // purchase order). The purchase order form calls it before saving, so a clash is shown
    // next to the field instead of only as a save error.
    public sealed record CheckSerialNumbersQuery(
        List<string> SerialNumbers,
        // Editing an order: its own pending serials are not a clash.
        int? ExcludePurchaseOrderId
    ) : IRequest<Result>;

    public sealed record CheckSerialNumbersRequest(List<string>? SerialNumbers, int? ExcludePurchaseOrderId);

    public sealed record CheckSerialNumbersResponse(IReadOnlyList<string> Existing);
}
