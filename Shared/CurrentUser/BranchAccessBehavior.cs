using MediatR;

namespace Inventory_Management_System.Shared.CurrentUser
{
    /// <summary>
    /// A write request that belongs to a branch. Staff are tied to one branch, so they may only
    /// create or change records of their own branch.
    /// </summary>
    public interface IBranchScopedRequest
    {
        /// <summary>True when a user tied to <paramref name="userBranchId"/> may send this request.</summary>
        bool IsAllowedForBranch(int userBranchId);
    }

    /// <summary>
    /// Checks <see cref="IBranchScopedRequest"/> before the handler runs. Reads were already
    /// limited by the query filters in AppDbContext; this closes the same gap for writes.
    /// Admins (no branch) are never limited.
    /// </summary>
    public class BranchAccessBehavior<TRequest, TResponse>(ICurrentUser _currentUser)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is IBranchScopedRequest scoped
                && _currentUser.BranchId is int userBranchId
                && !scoped.IsAllowedForBranch(userBranchId)
                && typeof(TResponse) == typeof(Result))
            {
                object denied = new Result
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Status = "Forbidden",
                    Message = "You can only work with your own branch."
                };
                return (TResponse)denied;
            }

            return await next();
        }
    }
}
