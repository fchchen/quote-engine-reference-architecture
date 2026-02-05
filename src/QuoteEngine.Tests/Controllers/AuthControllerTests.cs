using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuoteEngine.Api.Controllers;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _sut = new AuthController(_authServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Demo_ReturnsOkWithToken()
    {
        // Arrange
        var expectedResponse = new AuthResponse
        {
            Token = "demo-token",
            Username = "demo-user",
            Role = "Demo",
            Expiration = DateTime.UtcNow.AddHours(1)
        };

        _authServiceMock
            .Setup(x => x.GenerateDemoToken())
            .Returns(expectedResponse);

        // Act
        var result = _sut.Demo();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("demo-user", response.Username);
        Assert.Equal("Demo", response.Role);
    }

    [Fact]
    public void Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest { Username = "admin", Password = "admin123" };
        var expectedResponse = new AuthResponse
        {
            Token = "admin-token",
            Username = "admin",
            Role = "Admin",
            Expiration = DateTime.UtcNow.AddHours(1)
        };

        _authServiceMock
            .Setup(x => x.Authenticate(It.IsAny<LoginRequest>()))
            .Returns(expectedResponse);

        // Act
        var result = _sut.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("admin", response.Username);
    }

    [Fact]
    public void Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest { Username = "admin", Password = "wrong" };

        _authServiceMock
            .Setup(x => x.Authenticate(It.IsAny<LoginRequest>()))
            .Returns((AuthResponse?)null);

        // Act
        var result = _sut.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
    }
}
