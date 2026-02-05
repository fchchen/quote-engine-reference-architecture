using Microsoft.Extensions.Logging;
using Moq;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Services;

/// <summary>
/// Unit tests for InMemoryRateTableService.
/// Tests rate lookup with fallback logic and classification code retrieval.
/// </summary>
public class RateTableServiceTests
{
    private readonly InMemoryRateTableService _sut;

    public RateTableServiceTests()
    {
        var loggerMock = new Mock<ILogger<InMemoryRateTableService>>();
        _sut = new InMemoryRateTableService(loggerMock.Object);
    }

    #region GetRateAsync Tests

    [Fact]
    public async Task GetRateAsync_ExactMatch_ReturnsCorrectRate()
    {
        // Act
        var result = await _sut.GetRateAsync("CA", "8810", ProductType.WorkersCompensation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CA", result.StateCode);
        Assert.Equal("8810", result.ClassificationCode);
        Assert.Equal(ProductType.WorkersCompensation, result.ProductType);
        Assert.Equal(2.50m, result.BaseRate);
    }

    [Fact]
    public async Task GetRateAsync_CaliforniaPlumbing_ReturnsHigherRate()
    {
        // CA plumbing has higher rate (8.50) vs other states (6.25)
        // Act
        var result = await _sut.GetRateAsync("CA", "5183", ProductType.WorkersCompensation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8.50m, result.BaseRate);
    }

    [Fact]
    public async Task GetRateAsync_TexasPlumbing_ReturnsNonCaliforniaRate()
    {
        // Act
        var result = await _sut.GetRateAsync("TX", "5183", ProductType.WorkersCompensation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6.25m, result.BaseRate);
    }

    [Fact]
    public async Task GetRateAsync_FallsBackToStateDefault_WhenNoExactMatch()
    {
        // Act - Use a classification code that doesn't have a specific entry
        var result = await _sut.GetRateAsync("CA", "UNKNOWN_CODE", ProductType.WorkersCompensation);

        // Assert - Should fall back to CA DEFAULT for WorkersComp
        Assert.NotNull(result);
        Assert.Equal("CA", result.StateCode);
        Assert.Equal("DEFAULT", result.ClassificationCode);
        Assert.Equal(2.00m, result.BaseRate);
    }

    [Fact]
    public async Task GetRateAsync_FallsBackToProductDefault_WhenNoStateMatch()
    {
        // Act - Use a state that doesn't exist in rate table
        var result = await _sut.GetRateAsync("ZZ", "UNKNOWN_CODE", ProductType.WorkersCompensation);

        // Assert - Should fall back to DEFAULT/DEFAULT for WorkersComp
        Assert.NotNull(result);
        Assert.Equal("DEFAULT", result.StateCode);
        Assert.Equal("DEFAULT", result.ClassificationCode);
    }

    [Theory]
    [InlineData(ProductType.WorkersCompensation)]
    [InlineData(ProductType.GeneralLiability)]
    [InlineData(ProductType.BusinessOwnersPolicy)]
    [InlineData(ProductType.CommercialAuto)]
    [InlineData(ProductType.ProfessionalLiability)]
    [InlineData(ProductType.CyberLiability)]
    public async Task GetRateAsync_AllProductTypes_HaveDefaultRates(ProductType productType)
    {
        // Act - Use DEFAULT fallback
        var result = await _sut.GetRateAsync("DEFAULT", "DEFAULT", productType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.BaseRate > 0);
        Assert.True(result.MinPremium > 0);
    }

    [Fact]
    public async Task GetRateAsync_IncludesStateTaxRate()
    {
        // Act
        var result = await _sut.GetRateAsync("CA", "8810", ProductType.WorkersCompensation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0.0328m, result.StateTaxRate); // CA = 3.28%
    }

    [Theory]
    [InlineData("CA", 0.0328)]
    [InlineData("TX", 0.018)]
    [InlineData("NY", 0.035)]
    [InlineData("FL", 0.015)]
    [InlineData("IL", 0.035)]
    [InlineData("PA", 0.02)]
    [InlineData("OH", 0.015)]
    [InlineData("GA", 0.025)]
    [InlineData("NC", 0.02)]
    [InlineData("MI", 0.025)]
    public async Task GetRateAsync_CorrectStateTaxRates(string stateCode, double expectedRateDouble)
    {
        var expectedRate = (decimal)expectedRateDouble;

        // Act
        var result = await _sut.GetRateAsync(stateCode, "DEFAULT", ProductType.WorkersCompensation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedRate, result.StateTaxRate);
    }

    [Fact]
    public async Task GetRateAsync_OnlyReturnsActiveEntries()
    {
        // All entries in sample data are active, so any valid lookup should return active entry
        // Act
        var result = await _sut.GetRateAsync("CA", "8810", ProductType.WorkersCompensation);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetRateAsync_GeneralLiability_ReturnsRate()
    {
        // Act
        var result = await _sut.GetRateAsync("CA", "41677", ProductType.GeneralLiability);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5.50m, result.BaseRate);
        Assert.Equal(500m, result.MinPremium);
    }

    [Fact]
    public async Task GetRateAsync_CaseInsensitive_StateCode()
    {
        // Act
        var result = await _sut.GetRateAsync("ca", "8810", ProductType.WorkersCompensation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2.50m, result.BaseRate);
    }

    #endregion

    #region GetClassificationCodesAsync Tests

    [Fact]
    public async Task GetClassificationCodesAsync_WorkersComp_ReturnsCodes()
    {
        // Act
        var result = await _sut.GetClassificationCodesAsync(ProductType.WorkersCompensation);

        // Assert
        var codes = result.ToList();
        Assert.True(codes.Count >= 5);
        Assert.Contains(codes, c => c.Code == "8810" && c.Description == "Clerical Office Employees");
        Assert.Contains(codes, c => c.Code == "5183" && c.Description == "Plumbing - Residential");
    }

    [Fact]
    public async Task GetClassificationCodesAsync_GeneralLiability_ReturnsCodes()
    {
        // Act
        var result = await _sut.GetClassificationCodesAsync(ProductType.GeneralLiability);

        // Assert
        var codes = result.ToList();
        Assert.True(codes.Count >= 5);
        Assert.Contains(codes, c => c.Code == "41677" && c.Description == "Restaurant - No Liquor");
        Assert.Contains(codes, c => c.Code == "91111" && c.Description == "Retail Store");
    }

    [Fact]
    public async Task GetClassificationCodesAsync_OnlyReturnsActiveCodes()
    {
        // Act
        var result = await _sut.GetClassificationCodesAsync(ProductType.WorkersCompensation);

        // Assert
        Assert.All(result, c => Assert.True(c.IsActive));
    }

    [Fact]
    public async Task GetClassificationCodesAsync_OnlyReturnsCodesForRequestedProduct()
    {
        // Act
        var result = await _sut.GetClassificationCodesAsync(ProductType.GeneralLiability);

        // Assert
        Assert.All(result, c => Assert.Equal(ProductType.GeneralLiability, c.ProductType));
    }

    [Fact]
    public async Task GetClassificationCodesAsync_ResultsOrderedByCode()
    {
        // Act
        var result = await _sut.GetClassificationCodesAsync(ProductType.WorkersCompensation);

        // Assert
        var codes = result.Select(c => c.Code).ToList();
        var sortedCodes = codes.OrderBy(c => c).ToList();
        Assert.Equal(sortedCodes, codes);
    }

    [Fact]
    public async Task GetClassificationCodesAsync_EachCodeHasHazardGroup()
    {
        // Act
        var result = await _sut.GetClassificationCodesAsync(ProductType.WorkersCompensation);

        // Assert
        Assert.All(result, c => Assert.False(string.IsNullOrEmpty(c.HazardGroup)));
    }

    #endregion
}
