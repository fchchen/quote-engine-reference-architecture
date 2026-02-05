using Microsoft.Extensions.Configuration;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Services;

public class AuthServiceTests
{
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "QuoteEngineDemoSecretKey2024!AtLeast32Chars",
                ["Jwt:Issuer"] = "QuoteEngine.Api",
                ["Jwt:Audience"] = "QuoteEngine.Client",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();

        _sut = new AuthService(configuration);
    }

    [Fact]
    public void GenerateDemoToken_ReturnsValidToken()
    {
        // Act
        var result = _sut.GenerateDemoToken();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("demo-user", result.Username);
        Assert.Equal("Demo", result.Role);
        Assert.True(result.Expiration > DateTime.UtcNow);
    }

    [Theory]
    [InlineData("admin", "admin123", "Admin")]
    [InlineData("underwriter", "underwriter123", "Underwriter")]
    [InlineData("agent", "agent123", "Agent")]
    public void Authenticate_ValidCredentials_ReturnsToken(string username, string password, string expectedRole)
    {
        // Arrange
        var request = new LoginRequest { Username = username, Password = password };

        // Act
        var result = _sut.Authenticate(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal(username, result.Username);
        Assert.Equal(expectedRole, result.Role);
    }

    [Fact]
    public void Authenticate_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest { Username = "admin", Password = "wrong" };

        // Act
        var result = _sut.Authenticate(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Authenticate_UnknownUser_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest { Username = "nobody", Password = "password" };

        // Act
        var result = _sut.Authenticate(request);

        // Assert
        Assert.Null(result);
    }
}
