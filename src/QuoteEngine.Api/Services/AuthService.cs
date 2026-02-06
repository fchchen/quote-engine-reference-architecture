using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using QuoteEngine.Api.Models;

namespace QuoteEngine.Api.Services;

/// <summary>
/// Authentication service with hardcoded demo users.
/// Stateless — registered as Singleton.
/// </summary>
public class AuthService : IAuthService
{
    private readonly JwtSettings _jwtSettings;

    private static readonly Dictionary<string, (string Password, string Role)> DemoUsers = new()
    {
        ["admin"] = ("admin123", "Admin"),
        ["underwriter"] = ("underwriter123", "Underwriter"),
        ["agent"] = ("agent123", "Agent")
    };

    public AuthService(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings));
    }

    public AuthResponse? Authenticate(LoginRequest request)
    {
        if (!DemoUsers.TryGetValue(request.Username, out var user) || user.Password != request.Password)
            return null;

        return GenerateToken(request.Username, user.Role);
    }

    public AuthResponse GenerateDemoToken()
    {
        return GenerateToken("demo-user", "Demo");
    }

    private AuthResponse GenerateToken(string username, string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Username = username,
            Role = role,
            Expiration = expiration
        };
    }
}
