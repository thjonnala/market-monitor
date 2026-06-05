using MarketMonitor.Application.Dtos;
using MarketMonitor.Application.Interfaces;
using MarketMonitor.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MarketMonitor.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _tokens;

    public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService tokens)
    {
        _userManager = userManager;
        _tokens = tokens;
    }

    /// <summary>Register a new user. Identity hashes the password; plaintext is never stored.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userManager.FindByEmailAsync(email) is not null)
            return Conflict(new ProblemDetails { Status = 409, Title = "Conflict", Detail = "An account with that email already exists." });

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Registration failed",
                Detail = string.Join(" ", result.Errors.Select(e => e.Description))
            });

        return Ok(BuildAuthResponse(user));
    }

    /// <summary>Authenticate and receive a JWT bearer token.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);

        // Same response for unknown user and wrong password (avoid user enumeration).
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Unauthorized", Detail = "Invalid email or password." });

        return Ok(BuildAuthResponse(user));
    }

    private AuthResponse BuildAuthResponse(ApplicationUser user)
    {
        var (token, expiresAt) = _tokens.CreateToken(user.Id, user.Email!, user.DisplayName);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email!,
            DisplayName = user.DisplayName
        };
    }
}
