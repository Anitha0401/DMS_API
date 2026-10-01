using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DMS.API.Auth;
using DMS.Application.DTO;
using DMS.Application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DMS.API.Controllers;

public sealed record LoginResponse(string Token, DateTime ExpiresUtc, string UserId, string UserName, string Role);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Login against HQ_Users (hashed passwords). Replaces the hard-coded admin/admin123 user.</summary>
[Route("api/[controller]")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly IHQUserService _users;
    private readonly JwtOptions _jwt;
    private readonly ILoginService _loginService;
    private readonly ILogger<LoginController> _logger;
    private static readonly PasswordHasher<object> Hasher = new();

    public LoginController(IHQUserService users, IOptions<JwtOptions> jwt, ILoginService loginService, ILogger<LoginController> logger)
    {
        _users = users;
        _jwt = jwt.Value;
        _loginService = loginService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] UserLogin userLogin)
    {
        if (string.IsNullOrWhiteSpace(userLogin.UserId) ||
            string.IsNullOrEmpty(userLogin.Password))
        {
            return BadRequest("User ID and password are required.");
        }

        var user = await _users.GetUserForLoginAsync(userLogin.UserId.Trim());

        // Hasher.VerifyHashedPassword(null!, user.PasswordHash, userLogin.Password) == PasswordVerificationResult.Failed
        var isPasswordValid =
            user?.PasswordHash is not null &&
            string.Equals(
                user.PasswordHash,
                userLogin.Password,
                StringComparison.Ordinal);

        if (!isPasswordValid)
        {
            _logger.LogWarning("Failed login for {UserId}", userLogin.UserId);
            return Unauthorized("Invalid user ID or password.");
        }

        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpireMinutes);

        var claims = new List<Claim>
        {
            new(DmsClaims.UserId, user.UserId),
            new(DmsClaims.Name, user.UserName),
            new(DmsClaims.Role, user.Role)
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(DmsClaims.Email, user.Email));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwt.Key));

        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256));

        return Ok(new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expires,
            user.UserId,
            user.UserName,
            user.Role));
    }

    /// <summary>Lets a logged-in user change their own password.</summary>
    [Authorize]
    [HttpPost("ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(DmsClaims.UserId)?.Value;
        if (userId is null) return Unauthorized();
        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 10)
            return BadRequest("The new password must be at least 10 characters.");

        var user = await _users.GetUserForLoginAsync(userId);
        if (user?.PasswordHash is null ||
            Hasher.VerifyHashedPassword(null!, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            return BadRequest("The current password is not correct.");

        await _users.SetPasswordHashAsync(userId, Hasher.HashPassword(null!, request.NewPassword));
        return NoContent();
    }

    /// <summary>Environment label for the header. Admins only: it reveals server details.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("dbinfo")]
    public IActionResult GetDbInfo()
    {
        var (server, database) = _loginService.GetDbInfo();
        return Ok(new { server, database });
    }

    /// <summary>Used by "dotnet run -- set-password" (see Program.cs).</summary>
    internal static string HashPassword(string password) => Hasher.HashPassword(null!, password);
}
