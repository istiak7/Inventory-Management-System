using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Users.Shared.Services
{
    public interface ITokenService
    {
        public string GenerateJwtToken(User user);
        public string GenerateRefreshToken();
    }
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        public string GenerateRefreshToken() => Guid.NewGuid().ToString();
        public TokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }
        public string GenerateJwtToken(User user)
        {
            var claims = new[]
           {
                new Claim(JwtRegisteredClaimNames.Sub, _jwtSettings.Subject),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Name),
                new Claim("id", user.Id.ToString()),
                new Claim("roleId", user.RoleId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

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
