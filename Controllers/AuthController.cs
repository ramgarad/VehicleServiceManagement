using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VehicleService.Entities;

namespace VehicleServiceApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AuthController> _log;

    public AuthController(UserManager<ApplicationUser> users, IConfiguration cfg, ILogger<AuthController> log)
    {
        _users = users;
        _cfg = cfg;
        _log = log;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user == null || !await _users.CheckPasswordAsync(user, req.Password))
        {
            _log.LogWarning("Failed login attempt for {Email}", req.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var role = (await _users.GetRolesAsync(user)).FirstOrDefault() ?? "";
        var claims = new List<Claim>
        {
            new("sub", user.Id),
            new("email", user.Email ?? ""),
            new("name", user.FullName),
            new("role", role)
        };
        if (user.ServiceAdvisorId != null) claims.Add(new Claim("advisorId", user.ServiceAdvisorId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        _log.LogInformation("User {Email} logged in as {Role}", user.Email, role);
        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            email = user.Email,
            fullName = user.FullName,
            role,
            advisorId = user.ServiceAdvisorId
        });
    }
}
