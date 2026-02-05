using QuoteEngine.Api.Models;

namespace QuoteEngine.Api.Services;

public interface IAuthService
{
    AuthResponse? Authenticate(LoginRequest request);
    AuthResponse GenerateDemoToken();
}
