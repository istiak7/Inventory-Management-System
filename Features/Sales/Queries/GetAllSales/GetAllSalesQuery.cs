using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Queries.GetAllSales
{
    public sealed record GetAllSalesQuery(
        int PageNumber = 1,
        int PageSize = 20,
        int? CustomerId = null,
        int? BranchId = null,
        string? SaleType = null,   // Cash | Credit
        string? Search = null      // matches invoice number or customer name
    ) : IRequest<Result>;
}
