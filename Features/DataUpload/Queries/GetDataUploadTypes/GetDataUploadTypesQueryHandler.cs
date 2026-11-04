using Inventory_Management_System.Features.DataUpload.Shared;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.DataUpload.Queries.GetDataUploadTypes
{
    // Only the types this user may upload, so the dropdown never offers one that would be refused.
    public class GetDataUploadTypesQueryHandler : IRequestHandler<GetDataUploadTypesQuery, Result>
    {
        public Task<Result> Handle(GetDataUploadTypesQuery request, CancellationToken cancellationToken)
        {
            var types = DataUploadCatalog.All
                .Where(d => DataUploadCatalog.CanUpload(request.User, d))
                .Select(d => new DataUploadTypeResponse(
                    (int)d.Type,
                    d.Type.ToString(),
                    d.Name,
                    d.Description,
                    d.Columns.Select(c => new DataUploadColumnResponse(c.Order, c.Name, c.Required, c.Hint, c.MaxLength)).ToList()))
                .ToList();

            return Task.FromResult(new Result
            {
                IsSuccess = true,
                StatusCode = 200,
                Status = "Success",
                Message = "Upload types retrieved successfully",
                Data = types
            });
        }
    }
}
