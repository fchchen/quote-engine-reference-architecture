using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;

namespace QuoteEngine.Api.Controllers;

/// <summary>
/// Authentication controller for demo login and JWT token generation.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get a demo token without credentials.
    /// </summary>
    /// <returns>JWT token for demo-user</returns>
    [HttpPost("demo")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public ActionResult<AuthResponse> Demo()
    {
        _logger.LogInformation("Demo token requested");
        var response = _authService.GenerateDemoToken();
        return Ok(response);
    }

    /// <summary>
    /// Login with username and password.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token on success</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthResponse> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        var response = _authService.Authenticate(request);

        if (response is null)
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Detail = "Invalid username or password",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(response);
    }
}
