namespace Inventory_Management_System.Features.UnitsOfMeasure.Shared.Dtos
{
    public sealed record UnitOfMeasureResponse(
        int Id,
        string Name,
        string ShortName,
        string Description,
        // True = Active (can be picked for new products).
        bool IsActive,
        // How many products use this unit.
        int ProductCount,
        DateTime CreatedAt
    );
}
