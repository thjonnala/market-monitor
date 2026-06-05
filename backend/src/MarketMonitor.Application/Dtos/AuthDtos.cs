using System.ComponentModel.DataAnnotations;

namespace MarketMonitor.Application.Dtos;

public sealed class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(3), MaxLength(40)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthResponse
{
    public required string Token { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
}
