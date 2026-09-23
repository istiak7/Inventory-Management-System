using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Users.Command.ChangePassword
{
    public class ChangePasswordCommandHandler(
        AppDbContext _db,
        ILogger<ChangePasswordCommandHandler> _logger
    ) : IRequestHandler<ChangePasswordCommand, Result>
    {
        public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user is null)
            {
                return Fail(404, "User not found");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Fail(400, "Current password is not correct");
            }

            try
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
                return Fail(500, "An error occurred while changing the password.");
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
