using Microsoft.Extensions.Logging;
using Moq;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Services;

/// <summary>
/// Unit tests for QuoteService.
///
/// INTERVIEW TALKING POINTS:
/// - AAA pattern (Arrange, Act, Assert)
/// - Mocking dependencies with Moq
/// - Testing async methods
/// - Testing edge cases and error handling
/// </summary>
public class QuoteServiceTests
{
    private readonly Mock<IRiskCalculator> _riskCalculatorMock;
    private readonly Mock<IRateTableService> _rateTableServiceMock;
    private readonly Mock<ILogger<QuoteService>> _loggerMock;
    private readonly QuoteService _sut; // System Under Test

    public QuoteServiceTests()
    {
        _riskCalculatorMock = new Mock<IRiskCalculator>();
        _rateTableServiceMock = new Mock<IRateTableService>();
        _loggerMock = new Mock<ILogger<QuoteService>>();

        _sut = new QuoteService(
            _riskCalculatorMock.Object,
            _rateTableServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CalculateQuoteAsync_ValidRequest_ReturnsQuoteResponse()
    {
        // Arrange
        var request = CreateValidQuoteRequest();
        var rateEntry = CreateRateEntry();
        var riskAssessment = CreateStandardRiskAssessment();
        var premium = CreatePremiumBreakdown();

        _rateTableServiceMock
            .Setup(x => x.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProductType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rateEntry);

        _riskCalculatorMock
            .Setup(x => x.CalculateRisk(It.IsAny<QuoteRequest>()))
            .Returns(riskAssessment);

        _riskCalculatorMock
            .Setup(x => x.CalculatePremium(
                It.IsAny<QuoteRequest>(),
                It.IsAny<RiskAssessment>(),
                It.IsAny<RateTableEntry>()))
            .Returns(premium);

        // Act
        var result = await _sut.CalculateQuoteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.BusinessName, result.BusinessName);
        Assert.Equal(QuoteStatus.Quoted, result.Status);
        Assert.True(result.IsEligible);
        Assert.StartsWith("QT-", result.QuoteNumber);
    }

    [Fact]
    public async Task CalculateQuoteAsync_NewBusiness_ReturnsDeclined()
    {
        // Arrange
        var request = CreateValidQuoteRequest();
        request.YearsInBusiness = 0; // Less than 1 year = ineligible

        // Act
        var result = await _sut.CalculateQuoteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(QuoteStatus.Declined, result.Status);
        Assert.False(result.IsEligible);
        Assert.Contains(result.EligibilityMessages,
            m => m.Contains("1 year", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CalculateQuoteAsync_NoRateFound_ReturnsDeclined()
    {
        // Arrange
        var request = CreateValidQuoteRequest();

        _rateTableServiceMock
            .Setup(x => x.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProductType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateTableEntry?)null);

        // Act
        var result = await _sut.CalculateQuoteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(QuoteStatus.Declined, result.Status);
        Assert.False(result.IsEligible);
    }

    [Fact]
    public async Task CalculateQuoteAsync_HighRisk_ReturnsDeclined()
    {
        // Arrange
        var request = CreateValidQuoteRequest();
        var rateEntry = CreateRateEntry();
        var riskAssessment = new RiskAssessment
        {
            RiskScore = 85,
            RiskTier = RiskTier.Decline
        };

        _rateTableServiceMock
            .Setup(x => x.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProductType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rateEntry);

        _riskCalculatorMock
            .Setup(x => x.CalculateRisk(It.IsAny<QuoteRequest>()))
            .Returns(riskAssessment);

        _riskCalculatorMock
            .Setup(x => x.CalculatePremium(
                It.IsAny<QuoteRequest>(),
                It.IsAny<RiskAssessment>(),
                It.IsAny<RateTableEntry>()))
            .Returns(CreatePremiumBreakdown());

        // Act
        var result = await _sut.CalculateQuoteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(QuoteStatus.Declined, result.Status);
        Assert.False(result.IsEligible);
    }

    [Fact]
    public async Task CalculateQuoteAsync_WorkersCompNoEmployees_ReturnsDeclined()
    {
        // Arrange
        var request = CreateValidQuoteRequest();
        request.ProductType = ProductType.WorkersCompensation;
        request.EmployeeCount = 0;

        // Act
        var result = await _sut.CalculateQuoteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(QuoteStatus.Declined, result.Status);
        Assert.Contains(result.EligibilityMessages,
            m => m.Contains("employee", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetQuoteAsync_ExistingQuote_ReturnsQuote()
    {
        // Arrange - First create a quote
        var request = CreateValidQuoteRequest();
        var rateEntry = CreateRateEntry();
        var riskAssessment = CreateStandardRiskAssessment();
        var premium = CreatePremiumBreakdown();

        _rateTableServiceMock
            .Setup(x => x.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProductType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rateEntry);

        _riskCalculatorMock
            .Setup(x => x.CalculateRisk(It.IsAny<QuoteRequest>()))
            .Returns(riskAssessment);

        _riskCalculatorMock
            .Setup(x => x.CalculatePremium(
                It.IsAny<QuoteRequest>(),
                It.IsAny<RiskAssessment>(),
                It.IsAny<RateTableEntry>()))
            .Returns(premium);

        var createdQuote = await _sut.CalculateQuoteAsync(request);

        // Act
        var result = await _sut.GetQuoteAsync(createdQuote!.QuoteNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdQuote.QuoteNumber, result.QuoteNumber);
    }

    [Fact]
    public async Task GetQuoteAsync_NonExistentQuote_ReturnsNull()
    {
        // Act
        var result = await _sut.GetQuoteAsync("QT-NONEXISTENT-12345");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckEligibilityAsync_ValidBusiness_ReturnsEligible()
    {
        // Arrange
        var request = CreateValidQuoteRequest();

        // Act
        var result = await _sut.CheckEligibilityAsync(request);

        // Assert
        Assert.True(result.IsEligible);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public async Task CheckEligibilityAsync_HighPayroll_ReturnsWarning()
    {
        // Arrange
        var request = CreateValidQuoteRequest();
        request.AnnualPayroll = 15_000_000m; // Above $10M threshold

        // Act
        var result = await _sut.CheckEligibilityAsync(request);

        // Assert
        Assert.True(result.IsEligible);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings,
            w => w.Contains("underwriter", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetQuoteHistoryAsync_AfterCreatingQuote_ReturnsQuote()
    {
        // Arrange
        var request = CreateValidQuoteRequest();
        SetupMocksForSuccessfulQuote();

        var createdQuote = await _sut.CalculateQuoteAsync(request);
        Assert.NotNull(createdQuote);

        // Act
        var history = await _sut.GetQuoteHistoryAsync(request.TaxId);

        // Assert
        var historyList = history.ToList();
        Assert.NotEmpty(historyList);
        Assert.Contains(historyList, q => q.QuoteNumber == createdQuote.QuoteNumber);
    }

    [Fact]
    public async Task GetQuoteHistoryAsync_DifferentTaxIds_ReturnSeparateHistories()
    {
        // Arrange
        SetupMocksForSuccessfulQuote();

        var request1 = CreateValidQuoteRequest();
        request1.TaxId = "11-1111111";
        var quote1 = await _sut.CalculateQuoteAsync(request1);

        var request2 = CreateValidQuoteRequest();
        request2.TaxId = "22-2222222";
        var quote2 = await _sut.CalculateQuoteAsync(request2);

        // Act
        var history1 = (await _sut.GetQuoteHistoryAsync("11-1111111")).ToList();
        var history2 = (await _sut.GetQuoteHistoryAsync("22-2222222")).ToList();

        // Assert
        Assert.Single(history1);
        Assert.Single(history2);
        Assert.Equal(quote1!.QuoteNumber, history1[0].QuoteNumber);
        Assert.Equal(quote2!.QuoteNumber, history2[0].QuoteNumber);
    }

    [Fact]
    public async Task GetQuoteHistoryAsync_UnknownTaxId_ReturnsEmpty()
    {
        // Act
        var history = await _sut.GetQuoteHistoryAsync("99-9999999");

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public async Task CalculateQuoteAsync_TwoSequentialCalls_ProduceDistinctQuoteNumbers()
    {
        // Arrange
        var request = CreateValidQuoteRequest();
        var rateEntry = CreateRateEntry();
        var riskAssessment = CreateStandardRiskAssessment();
        var premium = CreatePremiumBreakdown();

        _rateTableServiceMock
            .Setup(x => x.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProductType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rateEntry);

        _riskCalculatorMock
            .Setup(x => x.CalculateRisk(It.IsAny<QuoteRequest>()))
            .Returns(riskAssessment);

        _riskCalculatorMock
            .Setup(x => x.CalculatePremium(
                It.IsAny<QuoteRequest>(),
                It.IsAny<RiskAssessment>(),
                It.IsAny<RateTableEntry>()))
            .Returns(premium);

        // Act
        var result1 = await _sut.CalculateQuoteAsync(request);
        var result2 = await _sut.CalculateQuoteAsync(request);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1.QuoteNumber, result2.QuoteNumber);
    }

    #region Helper Methods

    private void SetupMocksForSuccessfulQuote()
    {
        _rateTableServiceMock
            .Setup(x => x.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProductType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRateEntry());

        _riskCalculatorMock
            .Setup(x => x.CalculateRisk(It.IsAny<QuoteRequest>()))
            .Returns(CreateStandardRiskAssessment());

        _riskCalculatorMock
            .Setup(x => x.CalculatePremium(
                It.IsAny<QuoteRequest>(),
                It.IsAny<RiskAssessment>(),
                It.IsAny<RateTableEntry>()))
            .Returns(CreatePremiumBreakdown());
    }

    private static QuoteRequest CreateValidQuoteRequest()
    {
        return new QuoteRequest
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
    }

    private static RateTableEntry CreateRateEntry()
    {
        return new RateTableEntry
        {
            Id = 1,
            StateCode = "CA",
            ClassificationCode = "8810",
            ProductType = ProductType.GeneralLiability,
            BaseRate = 5.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m,
            EffectiveDate = DateTime.UtcNow.AddYears(-1),
            IsActive = true
        };
    }

    private static RiskAssessment CreateStandardRiskAssessment()
    {
        return new RiskAssessment
        {
            RiskScore = 45,
            RiskTier = RiskTier.Standard,
            FactorScores = new List<RiskFactorScore>
            {
                new() { FactorName = "Years in Business", Score = 25, Weight = 20, Impact = "Favorable" },
                new() { FactorName = "Employee Count", Score = 35, Weight = 15, Impact = "Neutral" },
                new() { FactorName = "Industry Risk", Score = 20, Weight = 25, Impact = "Favorable" }
            },
            Notes = new List<string> { "Standard risk profile" }
        };
    }

    private static PremiumBreakdown CreatePremiumBreakdown()
    {
        return new PremiumBreakdown
        {
            BasePremium = 5500m,
            Adjustments = new List<PremiumAdjustment>(),
            TotalAdjustments = 0m,
            Subtotal = 5500m,
            StateTax = 180.40m,
            PolicyFee = 150m,
            AnnualPremium = 5830.40m,
            MonthlyPremium = 485.87m,
            MinimumPremium = 500m
        };
    }

    #endregion
}
