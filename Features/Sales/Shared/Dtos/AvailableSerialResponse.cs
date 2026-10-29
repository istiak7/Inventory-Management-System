namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    // One in-stock unit of a serialized variant that the sale screen can pick from,
    // so the seller selects a serial number instead of typing it by hand.
    public sealed record AvailableSerialResponse(
        int ProductSerialId,
        string SerialNumber,
        int WarrantyMonths,
        DateTime ReceivedDate
    );
}
