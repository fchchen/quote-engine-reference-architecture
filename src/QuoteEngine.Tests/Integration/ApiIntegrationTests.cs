using System.Net;
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
        // Act
        var response = await _client.GetAsync("/api/v1/business/search?searchTerm=Pacific");

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
        // Act
        var response = await _client.GetAsync("/api/v1/business/search?stateCode=TX");

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
        // Act
        var response = await _client.GetAsync("/api/v1/business/search?pageSize=3&pageNumber=1");

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
        // Act
        var response = await _client.GetAsync("/api/v1/business/1");

        // Assert
        response.EnsureSuccessStatusCode();
        var business = await response.Content.ReadFromJsonAsync<BusinessInfo>(JsonOptions);
        Assert.NotNull(business);
        Assert.Equal("Sunrise Bakery LLC", business.BusinessName);
    }

    [Fact]
    public async Task BusinessGetById_NonExistentId_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/business/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BusinessGetByTaxId_ExistingTaxId_ReturnsBusiness()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/business/taxid/12-3456789");

        // Assert
        response.EnsureSuccessStatusCode();
        var business = await response.Content.ReadFromJsonAsync<BusinessInfo>(JsonOptions);
        Assert.NotNull(business);
        Assert.Equal("Sunrise Bakery LLC", business.BusinessName);
    }

    [Fact]
    public async Task BusinessGetByTaxId_NonExistentTaxId_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/business/taxid/99-9999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region RateTable Endpoints

    [Fact]
    public async Task GetStates_ReturnsListOfStates()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/ratetable/states");

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
        // Act
        var response = await _client.GetAsync("/api/v1/ratetable/products");

        // Assert
        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<ProductInfo>>(JsonOptions);
        Assert.NotNull(products);
        Assert.Equal(6, products.Count);
    }

    [Fact]
    public async Task GetBusinessTypes_ReturnsAllTypes()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/ratetable/business-types");

        // Assert
        response.EnsureSuccessStatusCode();
        var types = await response.Content.ReadFromJsonAsync<List<BusinessTypeInfo>>(JsonOptions);
        Assert.NotNull(types);
        Assert.Equal(Enum.GetValues<BusinessType>().Length, types.Count);
    }

    [Fact]
    public async Task GetClassificationCodes_WorkersComp_ReturnsCodes()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/ratetable/classifications/WorkersCompensation");

        // Assert
        response.EnsureSuccessStatusCode();
        var codes = await response.Content.ReadFromJsonAsync<List<ClassificationCode>>(JsonOptions);
        Assert.NotNull(codes);
        Assert.True(codes.Count >= 5);
    }

    [Fact]
    public async Task GetRate_ValidCriteria_ReturnsRate()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/v1/ratetable/rate?stateCode=CA&classificationCode=8810&productType=WorkersCompensation");

        // Assert
        response.EnsureSuccessStatusCode();
        var rate = await response.Content.ReadFromJsonAsync<RateTableEntry>(JsonOptions);
        Assert.NotNull(rate);
        Assert.Equal(2.50m, rate.BaseRate);
    }

    [Fact]
    public async Task GetRate_UnknownCriteria_FallsBackToDefault()
    {
        // Act - Unknown classification code should fall back to DEFAULT
        var response = await _client.GetAsync(
            "/api/v1/ratetable/rate?stateCode=CA&classificationCode=UNKNOWN&productType=WorkersCompensation");

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
        var request = new QuoteRequest
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/quote", request, JsonOptions);

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
        var request = new QuoteRequest
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/quote", request, JsonOptions);

        // Assert - Model validation rejects before reaching service
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndRetrieveQuote_RoundTrip()
    {
        // Arrange
        var request = new QuoteRequest
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
        var createResponse = await _client.PostAsJsonAsync("/api/v1/quote", request, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var createdQuote = await createResponse.Content.ReadFromJsonAsync<QuoteResponse>(JsonOptions);
        Assert.NotNull(createdQuote);

        // Act - Retrieve
        var getResponse = await _client.GetAsync($"/api/v1/quote/{createdQuote.QuoteNumber}");

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
        // Act
        var response = await _client.GetAsync("/api/v1/quote/QT-NONEXISTENT");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CheckEligibility_ValidBusiness_ReturnsEligible()
    {
        // Arrange
        var request = new QuoteRequest
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/quote/eligibility", request, JsonOptions);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EligibilityResult>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.IsEligible);
    }

    #endregion

    #region JSON Serialization

    [Fact]
    public async Task JsonResponse_UsesCamelCase()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/ratetable/states");
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
        // Act
        var response = await _client.GetAsync("/api/v1/ratetable/products");
        var json = await response.Content.ReadAsStringAsync();

        // Assert - Enum values should be strings, not numbers
        Assert.Contains("WorkersCompensation", json);
        Assert.Contains("GeneralLiability", json);
    }

    #endregion
}
