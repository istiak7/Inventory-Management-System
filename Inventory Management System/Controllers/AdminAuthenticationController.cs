using Inventory_Management_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Inventory_Management_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminAuthenticationController : ControllerBase
    {
        private readonly IAuthentication repository;
        public AdminAuthenticationController(IAuthentication repository)
        {
            this.repository = repository;
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
        {
            var token = await repository.AuthenticateUser(username, password);

            if (token == null)
                return Unauthorized("Invalid credentials");

            return Ok(new { token });
        }

    }
}
