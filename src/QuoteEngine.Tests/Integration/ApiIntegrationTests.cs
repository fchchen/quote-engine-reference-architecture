using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using QuoteEngine.Api.Controllers;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Integration;

/// <summary>
/// Integration tests using WebApplicationFactory.
/// Tests the full HTTP pipeline including middleware, routing, and serialization.
///
/// INTERVIEW TALKING POINTS:
/// - Tests the real HTTP pipeline (middleware, routing, model binding)
/// - Uses WebApplicationFactory for in-memory test server
/// - No mocks - tests actual service implementations
/// - Verifies JSON serialization/deserialization
/// </summary>
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    // Match the API's JSON serialization settings
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    #region Auth Helpers

    private async Task<HttpRequestMessage> CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        // Get a fresh token for each test to avoid sharing state issues
        var tokenResponse = await _client.PostAsync("/api/v1/auth/demo", null);
        tokenResponse.EnsureSuccessStatusCode();
        var auth = await tokenResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return request;
    }

    private async Task<string> GetDemoTokenAsync()
    {
        var response = await _client.PostAsync("/api/v1/auth/demo", null);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return auth!.Token;
    }

    #endregion

    #region Auth Endpoints

    [Fact]
    public async Task AuthDemo_ReturnsToken()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/auth/demo", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth);
        Assert.NotEmpty(auth.Token);
        Assert.Equal("demo-user", auth.Username);
        Assert.Equal("Demo", auth.Role);
    }

    [Fact]
    public async Task AuthLogin_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new LoginRequest { Username = "admin", Password = "admin123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest, JsonOptions);

        // Assert
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth);
        Assert.Equal("admin", auth.Username);
        Assert.Equal("Admin", auth.Role);
    }

    [Fact]
    public async Task AuthLogin_InvalidCredentials_Returns401()
    {
        // Arrange
        var loginRequest = new LoginRequest { Username = "admin", Password = "wrong" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_NoToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/business/search?searchTerm=Pacific");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Health Check

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region Business Endpoints

    [Fact]
    public async Task BusinessSearch_ReturnsResults()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/business/search?searchTerm=Pacific");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BusinessSearchResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Single(result.Businesses);
        Assert.Equal("Pacific Coast Brewing Co", result.Businesses[0].BusinessName);
    }

    [Fact]
    public async Task BusinessSearch_WithStateFilter_ReturnsFilteredResults()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/business/search?stateCode=TX");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BusinessSearchResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Businesses, b => Assert.Equal("TX", b.StateCode));
    }

    [Fact]
    public async Task BusinessSearch_Pagination_Works()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/business/search?pageSize=3&pageNumber=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BusinessSearchResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(3, result.Businesses.Count);
        Assert.Equal(15, result.TotalCount);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task BusinessGetById_ExistingId_ReturnsBusiness()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/business/1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var business = await response.Content.ReadFromJsonAsync<BusinessInfo>(JsonOptions);
        Assert.NotNull(business);
        Assert.Equal("Sunrise Bakery LLC", business.BusinessName);
    }

    [Fact]
    public async Task BusinessGetById_NonExistentId_Returns404()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/business/999");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BusinessGetByTaxId_ExistingTaxId_ReturnsBusiness()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/business/taxid/12-3456789");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var business = await response.Content.ReadFromJsonAsync<BusinessInfo>(JsonOptions);
        Assert.NotNull(business);
        Assert.Equal("Sunrise Bakery LLC", business.BusinessName);
    }

    [Fact]
    public async Task BusinessGetByTaxId_NonExistentTaxId_Returns404()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/business/taxid/99-9999999");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region RateTable Endpoints

    [Fact]
    public async Task GetStates_ReturnsListOfStates()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ratetable/states");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var states = await response.Content.ReadFromJsonAsync<List<StateInfo>>(JsonOptions);
        Assert.NotNull(states);
        Assert.True(states.Count >= 10);
        Assert.Contains(states, s => s.Code == "CA");
    }

    [Fact]
    public async Task GetProducts_ReturnsSixProducts()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ratetable/products");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<ProductInfo>>(JsonOptions);
        Assert.NotNull(products);
        Assert.Equal(6, products.Count);
    }

    [Fact]
    public async Task GetBusinessTypes_ReturnsAllTypes()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ratetable/business-types");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var types = await response.Content.ReadFromJsonAsync<List<BusinessTypeInfo>>(JsonOptions);
        Assert.NotNull(types);
        Assert.Equal(Enum.GetValues<BusinessType>().Length, types.Count);
    }

    [Fact]
    public async Task GetClassificationCodes_WorkersComp_ReturnsCodes()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ratetable/classifications/WorkersCompensation");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var codes = await response.Content.ReadFromJsonAsync<List<ClassificationCode>>(JsonOptions);
        Assert.NotNull(codes);
        Assert.True(codes.Count >= 5);
    }

    [Fact]
    public async Task GetRate_ValidCriteria_ReturnsRate()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get,
            "/api/v1/ratetable/rate?stateCode=CA&classificationCode=8810&productType=WorkersCompensation");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var rate = await response.Content.ReadFromJsonAsync<RateTableEntry>(JsonOptions);
        Assert.NotNull(rate);
        Assert.Equal(2.50m, rate.BaseRate);
    }

    [Fact]
    public async Task GetRate_UnknownCriteria_FallsBackToDefault()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get,
            "/api/v1/ratetable/rate?stateCode=CA&classificationCode=UNKNOWN&productType=WorkersCompensation");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var rate = await response.Content.ReadFromJsonAsync<RateTableEntry>(JsonOptions);
        Assert.NotNull(rate);
        Assert.True(rate.BaseRate > 0);
    }

    #endregion

    #region Quote Endpoints

    [Fact]
    public async Task CreateQuote_ValidRequest_ReturnsQuote()
    {
        // Arrange
        var token = await GetDemoTokenAsync();
        var quoteRequest = new QuoteRequest
        {
            BusinessName = "Test Business LLC",
            TaxId = "12-3456789",
            BusinessType = BusinessType.Technology,
            StateCode = "CA",
            ClassificationCode = "8810",
            ProductType = ProductType.GeneralLiability,
            AnnualPayroll = 500000m,
            AnnualRevenue = 1000000m,
            EmployeeCount = 10,
            YearsInBusiness = 5,
            CoverageLimit = 1000000m,
            Deductible = 1000m
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/quote")
        {
            Content = JsonContent.Create(quoteRequest, options: JsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<QuoteResponse>(JsonOptions);
        Assert.NotNull(quote);
        Assert.StartsWith("QT-", quote.QuoteNumber);
        Assert.Equal("Test Business LLC", quote.BusinessName);
        Assert.True(quote.IsEligible);
        Assert.True(quote.Premium.AnnualPremium > 0);
    }

    [Fact]
    public async Task CreateQuote_InvalidRequest_Returns400()
    {
        // Arrange - YearsInBusiness=0 violates [Range(1,50)] model validation
        var token = await GetDemoTokenAsync();
        var quoteRequest = new QuoteRequest
        {
            BusinessName = "Brand New LLC",
            TaxId = "99-8765432",
            BusinessType = BusinessType.Technology,
            StateCode = "CA",
            ClassificationCode = "8810",
            ProductType = ProductType.GeneralLiability,
            AnnualPayroll = 500000m,
            AnnualRevenue = 1000000m,
            EmployeeCount = 10,
            YearsInBusiness = 0,
            CoverageLimit = 1000000m,
            Deductible = 1000m
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/quote")
        {
            Content = JsonContent.Create(quoteRequest, options: JsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert - Model validation rejects before reaching service
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndRetrieveQuote_RoundTrip()
    {
        // Arrange
        var token = await GetDemoTokenAsync();
        var quoteRequest = new QuoteRequest
        {
            BusinessName = "RoundTrip Test LLC",
            TaxId = "12-3456789",
            BusinessType = BusinessType.Office,
            StateCode = "CA",
            ClassificationCode = "8810",
            ProductType = ProductType.GeneralLiability,
            AnnualPayroll = 500000m,
            AnnualRevenue = 1000000m,
            EmployeeCount = 10,
            YearsInBusiness = 5,
            CoverageLimit = 1000000m,
            Deductible = 1000m
        };

        // Act - Create
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/quote")
        {
            Content = JsonContent.Create(quoteRequest, options: JsonOptions)
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdQuote = await createResponse.Content.ReadFromJsonAsync<QuoteResponse>(JsonOptions);
        Assert.NotNull(createdQuote);

        // Act - Retrieve
        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/quote/{createdQuote.QuoteNumber}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getResponse = await _client.SendAsync(getRequest);

        // Assert
        getResponse.EnsureSuccessStatusCode();
        var retrievedQuote = await getResponse.Content.ReadFromJsonAsync<QuoteResponse>(JsonOptions);
        Assert.NotNull(retrievedQuote);
        Assert.Equal(createdQuote.QuoteNumber, retrievedQuote.QuoteNumber);
        Assert.Equal(createdQuote.Premium.AnnualPremium, retrievedQuote.Premium.AnnualPremium);
    }

    [Fact]
    public async Task GetQuote_NonExistent_Returns404()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/quote/QT-NONEXISTENT");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CheckEligibility_ValidBusiness_ReturnsEligible()
    {
        // Arrange
        var token = await GetDemoTokenAsync();
        var quoteRequest = new QuoteRequest
        {
            BusinessName = "Eligible LLC",
            TaxId = "12-3456789",
            BusinessType = BusinessType.Technology,
            StateCode = "CA",
            ClassificationCode = "8810",
            ProductType = ProductType.GeneralLiability,
            AnnualPayroll = 500000m,
            AnnualRevenue = 1000000m,
            EmployeeCount = 10,
            YearsInBusiness = 5,
            CoverageLimit = 1000000m,
            Deductible = 1000m
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/quote/eligibility")
        {
            Content = JsonContent.Create(quoteRequest, options: JsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EligibilityResult>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.IsEligible);
    }

    #endregion

    #region Quote History

    [Fact]
    public async Task GetQuoteHistory_AfterCreatingQuote_ReturnsNonEmpty()
    {
        // Arrange - Create a quote first
        var token = await GetDemoTokenAsync();
        var taxId = "12-3456789";
        var quoteRequest = new QuoteRequest
        {
            BusinessName = "History Test LLC",
            TaxId = taxId,
            BusinessType = BusinessType.Technology,
            StateCode = "CA",
            ClassificationCode = "8810",
            ProductType = ProductType.GeneralLiability,
            AnnualPayroll = 500000m,
            AnnualRevenue = 1000000m,
            EmployeeCount = 10,
            YearsInBusiness = 5,
            CoverageLimit = 1000000m,
            Deductible = 1000m
        };

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/quote")
        {
            Content = JsonContent.Create(quoteRequest, options: JsonOptions)
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Act - Get history
        var historyRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/quote/history/{taxId}");
        historyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var historyResponse = await _client.SendAsync(historyRequest);

        // Assert
        historyResponse.EnsureSuccessStatusCode();
        var history = await historyResponse.Content.ReadFromJsonAsync<List<QuoteResponse>>(JsonOptions);
        Assert.NotNull(history);
        Assert.NotEmpty(history);
    }

    #endregion

    #region Premium Estimate

    [Fact]
    public async Task PremiumEstimate_ValidRequest_ReturnsEstimate()
    {
        // Arrange
        var token = await GetDemoTokenAsync();
        var estimateRequest = new PremiumEstimateRequest
        {
            ProductType = ProductType.GeneralLiability,
            StateCode = "CA",
            AnnualRevenue = 1000000m,
            AnnualPayroll = 500000m,
            EmployeeCount = 10,
            CoverageLimit = 1000000m,
            Deductible = 1000m
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/premium/estimate")
        {
            Content = JsonContent.Create(estimateRequest, options: JsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var estimate = await response.Content.ReadFromJsonAsync<PremiumEstimateResponse>(JsonOptions);
        Assert.NotNull(estimate);
        Assert.True(estimate.EstimatedAnnualPremium > 0);
        Assert.True(estimate.EstimatedMonthlyPremium > 0);
        Assert.Contains("estimate", estimate.Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PremiumEstimate_NoAuth_Returns401()
    {
        // Arrange
        var estimateRequest = new PremiumEstimateRequest
        {
            ProductType = ProductType.GeneralLiability,
            StateCode = "CA",
            CoverageLimit = 1000000m,
            Deductible = 1000m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/premium/estimate", estimateRequest, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region JSON Serialization

    [Fact]
    public async Task JsonResponse_UsesCamelCase()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ratetable/states");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        // Assert - Properties should be camelCase
        Assert.Contains("\"code\"", json);
        Assert.Contains("\"name\"", json);
        // Should NOT be PascalCase
        Assert.DoesNotContain("\"Code\"", json);
        Assert.DoesNotContain("\"Name\"", json);
    }

    [Fact]
    public async Task JsonResponse_EnumsAreStrings()
    {
        // Arrange
        var token = await GetDemoTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ratetable/products");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        // Assert - Enum values should be strings, not numbers
        Assert.Contains("WorkersCompensation", json);
        Assert.Contains("GeneralLiability", json);
    }

    #endregion
}
