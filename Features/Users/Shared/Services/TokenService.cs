using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Users.Shared.Services
{
    public interface ITokenService
    {
        public string GenerateJwtToken(User user, string roleName, IEnumerable<string> permissions);
        public string GenerateRefreshToken();
    }
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        // 32 random bytes: cannot be guessed. Only its hash is stored (see HashRefreshToken).
        public string GenerateRefreshToken() =>
            Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

        // SHA-256 is enough here: the token is long and random (no dictionary to guess from),
        // and unlike BCrypt the same token always gives the same hash, so it can be looked up.
        public static string HashRefreshToken(string refreshToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        public TokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }
        public string GenerateJwtToken(User user, string roleName, IEnumerable<string> permissions)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new("username", user.Name),
                new("id", user.Id.ToString()),
                new("roleId", user.RoleId.ToString()),
                // Standard role claim so [Authorize(Roles=...)] / RequireRole works.
                new(ClaimTypes.Role, roleName),
                // BranchId ("" when the user can access all branches, e.g. admin).
                new("branchId", user.BranchId?.ToString() ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // One "permission" claim per effective permission; endpoints check these.
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
