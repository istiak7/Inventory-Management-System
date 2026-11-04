using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Branches.Command.UpdateBranch
{
    public class UpdateBranchCommandHandler(
        AppDbContext _db,
        ILogger<UpdateBranchCommandHandler> _logger
    ) : IRequestHandler<UpdateBranchCommand, Result>
    {
        public async Task<Result> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var branch = await _db.Branches
                .FirstOrDefaultAsync(b => b.Id == request.Id && b.IsActive != deleted, cancellationToken);
            if (branch is null) return Fail(404, "Branch not found");

            var name = request.Name.Trim();
            if (await _db.Branches.AnyAsync(
                    b => b.Name.ToLower() == name.ToLower() && b.Id != branch.Id && b.IsActive != deleted,
                    cancellationToken))
                return Fail(400, "Branch already exists");

            try
            {
                branch.Name = name;
                branch.Location = request.Location;
                branch.PhoneNumber = request.PhoneNumber;
                branch.Email = request.Email;
                branch.UpDatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Branch updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch {BranchId}", request.Id);
                return Fail(500, "An error occurred while updating the branch.");
            }
        }

        private static Result Fail(int statusCode, string message) => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Status = "Error",
            Message = message
        };
    }
}
