using Inventory_Management_System.ApplicationDb;
using Inventory_Management_System.Models;
using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Inventory_Management_System.Repositories.Implementations
{
    public class AdminAuthenticationRepository : IAuthentication
    {
        private readonly IConfiguration config;
        private readonly ApplicationDbContext context;
        private readonly IPasswordHasher<User> passwordHasher;

        public AdminAuthenticationRepository(
            IConfiguration config,
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher)
        {
            this.config = config;
            this.context = context;
            this.passwordHasher = passwordHasher;
        }

        public async Task<string> AuthenticateUser(string username, string Password)
        {
            System.Diagnostics.Debug.WriteLine(username);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.Password, Password);
            if (verificationResult != PasswordVerificationResult.Success) return null;

            var token = GenerateJwtToken(user.Username, user.Role);
            return token;
        }

        public string GenerateJwtToken(string username, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,username),
                new Claim(ClaimTypes.Role, role)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
