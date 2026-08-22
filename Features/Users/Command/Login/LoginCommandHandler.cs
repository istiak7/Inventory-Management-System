using MediatR;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Users.Shared.Services;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;

namespace Inventory_Management_System.Features.Users.Login
{
    public class LoginCommandHandler(
        IBaseRepository<User> _userRepository,
        IBaseRepository<Role> _roleRepository,
        AppDbContext _db,
        ITokenService tokenService
    )
    : IRequestHandler<LoginUserCommand, Result>,
      IRequestHandler<RefreshTokenCommand, Result>
    {
        public async Task<Result> Handle(LoginUserCommand request, CancellationToken token)
        {
            var user = await _userRepository.GetAsync(u => u.Name == request.Identifier || u.Email == request.Identifier);    

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 401,
                    Status = "Error",
                    Message = "Invalid email or password"
                };
            }
            return await GenerateLoginResponseAsync(user);
        }

        public async Task<Result> Handle(RefreshTokenCommand request, CancellationToken token)
        {
            var user = await _userRepository.GetAsync(u => u.Id == request.UserId);
            if (user == null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "User not found"
                };
            }
            if (user.RefreshToken != BCrypt.Net.BCrypt.HashPassword(request.RefreshToken) || user.RefreshTokenExpireTime < DateTime.UtcNow)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 401,
                    Status = "Error",
                    Message = "Invalid or expired refresh token"
                };
            }
            return await GenerateLoginResponseAsync(user);
        }

        private async Task<Result> GenerateLoginResponseAsync(User user)
        {
            var role = await _roleRepository.GetAsync(r => r.Id == user.RoleId, asNoTracking: true);
            var roleName = role?.Name ?? string.Empty;

            var permissions = await GetEffectivePermissionsAsync(user);

            var RefreshToken = tokenService.GenerateRefreshToken();
            var jwtToken = tokenService.GenerateJwtToken(user, roleName, permissions);

            user.RefreshToken = BCrypt.Net.BCrypt.HashPassword(RefreshToken);
            user.RefreshTokenExpireTime = DateTime.UtcNow.AddDays(15);

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

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
                    RefreshToken = RefreshToken
                }
            };
        }

        // Effective access = permissions from the role + permissions granted to the user directly.
        private async Task<List<string>> GetEffectivePermissionsAsync(User user)
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
                .ToListAsync();
        }
    }
}
