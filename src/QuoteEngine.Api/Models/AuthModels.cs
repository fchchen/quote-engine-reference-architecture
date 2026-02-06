using System.ComponentModel.DataAnnotations;

namespace QuoteEngine.Api.Models;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}

public class JwtSettings
{
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = "QuoteEngine.Api";
    public string Audience { get; init; } = "QuoteEngine.Client";
    public int ExpirationMinutes { get; init; } = 60;
}
