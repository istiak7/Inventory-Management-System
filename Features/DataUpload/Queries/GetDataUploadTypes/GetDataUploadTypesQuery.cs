using Inventory_Management_System.Shared;
using MediatR;
using System.Security.Claims;

namespace Inventory_Management_System.Features.DataUpload.Queries.GetDataUploadTypes
{
    // The user is passed in so only the types they may upload are returned.
    public sealed record GetDataUploadTypesQuery(ClaimsPrincipal User) : IRequest<Result>;
}
