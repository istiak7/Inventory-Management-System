using MediatR;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Users.Shared.Services;
using Inventory_Management_System.Shared;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Users.Login
{
    public class LoginCommandHandler(
        AppDbContext _db,
        ITokenService tokenService
    )
    : IRequestHandler<LoginUserCommand, Result>,
      IRequestHandler<RefreshTokenCommand, Result>,
      IRequestHandler<LogoutCommand, Result>
    {
        // How long a refresh token (the "stay signed in" token) is valid.
        private const int RefreshTokenDays = 15;

        public async Task<Result> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var identifier = (request.Identifier ?? string.Empty).Trim();
            var email = identifier.ToLowerInvariant();

            // Email is matched without caring about upper/lower case. Email wins over a user name,
            // so a user name that looks like someone's email can never log in as them.
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken)
                       ?? await _db.Users.FirstOrDefaultAsync(u => u.Name == identifier, cancellationToken);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password ?? string.Empty, user.PasswordHash))
                return Fail(401, "Invalid email or password");

            if (user.IsActive != (int)EntityStatus.Active)
                return Fail(403, "This account is disabled. Please contact your administrator.");

            return await GenerateLoginResponseAsync(user, replacing: null, cancellationToken);
        }

        public async Task<Result> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Fail(401, "Invalid or expired refresh token");

            var hash = TokenService.HashRefreshToken(request.RefreshToken);
            var stored = await _db.UserRefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

            if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
                return Fail(401, "Invalid or expired refresh token");

            // A deactivated user cannot stay signed in.
            if (stored.User.IsActive != (int)EntityStatus.Active)
                return Fail(401, "This account is disabled.");

            // A new refresh token is issued every time; the used one is removed.
            return await GenerateLoginResponseAsync(stored.User, replacing: stored, cancellationToken);
        }

        public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                // No token sent: sign this user out everywhere.
                await RefreshTokenStore.RevokeAllAsync(_db, request.UserId, keepRefreshToken: null, cancellationToken);
            }
            else
            {
                // Sign out this device only.
                var hash = TokenService.HashRefreshToken(request.RefreshToken);
                var tokens = await _db.UserRefreshTokens
                    .Where(t => t.UserId == request.UserId && t.TokenHash == hash)
                    .ToListAsync(cancellationToken);
                _db.UserRefreshTokens.RemoveRange(tokens);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Logged out" };
        }

        private async Task<Result> GenerateLoginResponseAsync(
            User user,
            UserRefreshToken? replacing,
            CancellationToken cancellationToken)
        {
            var roleName = await _db.Roles
                .Where(r => r.Id == user.RoleId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            var permissions = await GetEffectivePermissionsAsync(user, cancellationToken);

            var refreshToken = tokenService.GenerateRefreshToken();
            var jwtToken = tokenService.GenerateJwtToken(user, roleName, permissions);

            // One token row per device. Expired rows of this user are cleaned up on the way.
            var expired = await _db.UserRefreshTokens
                .Where(t => t.UserId == user.Id && t.ExpiresAt < DateTime.UtcNow)
                .ToListAsync(cancellationToken);
            _db.UserRefreshTokens.RemoveRange(expired);
            if (replacing != null && !expired.Contains(replacing))
                _db.UserRefreshTokens.Remove(replacing);

            await _db.UserRefreshTokens.AddAsync(new UserRefreshToken
            {
                UserId = user.Id,
                TokenHash = TokenService.HashRefreshToken(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
            }, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new Result
            {
                IsSuccess = true,
                StatusCode = 200,
                Status = "Success",
                Message = "User logged in successfully",
                Data = new LoginResponse
                {
                    UserName = user.Name,
                    Email = user.Email,
                    RoleId = user.RoleId,
                    Role = roleName,
                    BranchId = user.BranchId,
                    AccessToken = jwtToken,
                    RefreshToken = refreshToken
                }
            };
        }

        // Effective access = permissions from the role + permissions granted to the user directly.
        private async Task<List<string>> GetEffectivePermissionsAsync(User user, CancellationToken cancellationToken)
        {
            var rolePermissionIds = _db.RolePermissions
                .Where(rp => rp.RoleId == user.RoleId)
                .Select(rp => rp.PermissionId);

            var userPermissionIds = _db.UserPermissions
                .Where(up => up.UserId == user.Id)
                .Select(up => up.PermissionId);

            return await _db.Permissions
                .Where(p => rolePermissionIds.Contains(p.Id) || userPermissionIds.Contains(p.Id))
                .Select(p => p.Name)
                .Distinct()
                .ToListAsync(cancellationToken);
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
