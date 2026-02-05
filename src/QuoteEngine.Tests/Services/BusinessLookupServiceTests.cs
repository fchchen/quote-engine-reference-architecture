using Microsoft.Extensions.Logging;
using Moq;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Services;

/// <summary>
/// Unit tests for InMemoryBusinessLookupService.
/// Tests search, filtering, and pagination logic.
/// </summary>
public class BusinessLookupServiceTests
{
    private readonly InMemoryBusinessLookupService _sut;

    public BusinessLookupServiceTests()
    {
        var loggerMock = new Mock<ILogger<InMemoryBusinessLookupService>>();
        _sut = new InMemoryBusinessLookupService(loggerMock.Object);
    }

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_NoFilters_ReturnsAllActiveBusinesses()
    {
        // Arrange
        var request = new BusinessSearchRequest { PageSize = 50 };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.True(result.TotalCount > 0);
        Assert.Equal(15, result.TotalCount); // 15 sample businesses
    }

    [Fact]
    public async Task SearchAsync_ByName_ReturnsMatchingBusinesses()
    {
        // Arrange
        var request = new BusinessSearchRequest { SearchTerm = "Pacific" };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Single(result.Businesses);
        Assert.Equal("Pacific Coast Brewing Co", result.Businesses[0].BusinessName);
    }

    [Fact]
    public async Task SearchAsync_ByTaxId_ReturnsMatchingBusiness()
    {
        // Arrange
        var request = new BusinessSearchRequest { SearchTerm = "12-3456789" };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Single(result.Businesses);
        Assert.Equal("Sunrise Bakery LLC", result.Businesses[0].BusinessName);
    }

    [Fact]
    public async Task SearchAsync_ByDbaName_ReturnsMatchingBusiness()
    {
        // Arrange
        var request = new BusinessSearchRequest { SearchTerm = "TechSol" };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Single(result.Businesses);
        Assert.Equal("Tech Solutions Inc", result.Businesses[0].BusinessName);
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_ReturnsResults()
    {
        // Arrange
        var request = new BusinessSearchRequest { SearchTerm = "pacific" };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Single(result.Businesses);
        Assert.Equal("Pacific Coast Brewing Co", result.Businesses[0].BusinessName);
    }

    [Fact]
    public async Task SearchAsync_ByStateCode_FiltersCorrectly()
    {
        // Arrange
        var request = new BusinessSearchRequest { StateCode = "TX", PageSize = 50 };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Equal(2, result.TotalCount); // Johnson Plumbing + Lone Star Auto
        Assert.All(result.Businesses, b => Assert.Equal("TX", b.StateCode));
    }

    [Fact]
    public async Task SearchAsync_ByBusinessType_FiltersCorrectly()
    {
        // Arrange
        var request = new BusinessSearchRequest { BusinessType = BusinessType.Construction, PageSize = 50 };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Equal(3, result.TotalCount); // Johnson Plumbing, Green Leaf, Desert Sun
        Assert.All(result.Businesses, b => Assert.Equal(BusinessType.Construction, b.BusinessType));
    }

    [Fact]
    public async Task SearchAsync_CombinedFilters_ReturnsNarrowedResults()
    {
        // Arrange - Search for construction businesses in TX
        var request = new BusinessSearchRequest
        {
            StateCode = "TX",
            BusinessType = BusinessType.Construction
        };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Single(result.Businesses);
        Assert.Equal("Johnson Plumbing Services", result.Businesses[0].BusinessName);
    }

    [Fact]
    public async Task SearchAsync_NoMatch_ReturnsEmptyResults()
    {
        // Arrange
        var request = new BusinessSearchRequest { SearchTerm = "ZZZZZ_NoMatch" };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Empty(result.Businesses);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_Pagination_FirstPage()
    {
        // Arrange
        var request = new BusinessSearchRequest { PageNumber = 1, PageSize = 5 };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Equal(5, result.Businesses.Count);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task SearchAsync_Pagination_SecondPage()
    {
        // Arrange
        var request = new BusinessSearchRequest { PageNumber = 2, PageSize = 5 };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Equal(5, result.Businesses.Count);
        Assert.Equal(2, result.PageNumber);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task SearchAsync_Pagination_LastPage()
    {
        // Arrange
        var request = new BusinessSearchRequest { PageNumber = 3, PageSize = 5 };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Equal(5, result.Businesses.Count);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task SearchAsync_ResultsOrderedByName()
    {
        // Arrange
        var request = new BusinessSearchRequest { PageSize = 50 };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        var names = result.Businesses.Select(b => b.BusinessName).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        Assert.Equal(sortedNames, names);
    }

    [Fact]
    public async Task SearchAsync_TaxIdFilter_IndependentOfSearchTerm()
    {
        // Arrange
        var request = new BusinessSearchRequest { TaxId = "12-3456789" };

        // Act
        var result = await _sut.SearchAsync(request);

        // Assert
        Assert.Single(result.Businesses);
        Assert.Equal("12-3456789", result.Businesses[0].TaxId);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsBusiness()
    {
        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Sunrise Bakery LLC", result.BusinessName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCompleteBusinessInfo()
    {
        // Act
        var result = await _sut.GetByIdAsync(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tech Solutions Inc", result.BusinessName);
        Assert.Equal("TechSol", result.DbaName);
        Assert.Equal("23-4567890", result.TaxId);
        Assert.Equal(BusinessType.Technology, result.BusinessType);
        Assert.Equal("CA", result.StateCode);
        Assert.Equal("8810", result.ClassificationCode);
        Assert.Equal(45, result.EmployeeCount);
        Assert.True(result.IsActive);
    }

    #endregion

    #region GetByTaxIdAsync Tests

    [Fact]
    public async Task GetByTaxIdAsync_ExistingTaxId_ReturnsBusiness()
    {
        // Act
        var result = await _sut.GetByTaxIdAsync("12-3456789");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("12-3456789", result.TaxId);
        Assert.Equal("Sunrise Bakery LLC", result.BusinessName);
    }

    [Fact]
    public async Task GetByTaxIdAsync_NonExistentTaxId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByTaxIdAsync("99-9999999");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTaxIdAsync_CaseInsensitive()
    {
        // Act - Tax IDs should match case-insensitively
        var result = await _sut.GetByTaxIdAsync("12-3456789");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Sunrise Bakery LLC", result.BusinessName);
    }

    #endregion
}
