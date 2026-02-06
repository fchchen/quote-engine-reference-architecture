using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuoteEngine.Api.Controllers;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Controllers;

public class PremiumControllerTests
{
    private readonly Mock<IRateTableService> _rateTableServiceMock;
    private readonly Mock<IRiskCalculator> _riskCalculatorMock;
    private readonly Mock<ILogger<PremiumController>> _loggerMock;
    private readonly PremiumController _sut;

    public PremiumControllerTests()
    {
        _rateTableServiceMock = new Mock<IRateTableService>();
        _riskCalculatorMock = new Mock<IRiskCalculator>();
        _loggerMock = new Mock<ILogger<PremiumController>>();

        _sut = new PremiumController(
            _rateTableServiceMock.Object,
            _riskCalculatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Estimate_ValidRequest_Returns200WithEstimate()
    {
        // Arrange
        var request = new PremiumEstimateRequest
        {
            ProductType = ProductType.GeneralLiability,
            StateCode = "CA",
            AnnualRevenue = 1000000m,
            EmployeeCount = 10,
            CoverageLimit = 1000000m,
            Deductible = 1000m
        };

        var rateEntry = new RateTableEntry
        {
            Id = 1,
            StateCode = "CA",
            ClassificationCode = "DEFAULT",
            ProductType = ProductType.GeneralLiability,
            BaseRate = 5.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m,
            EffectiveDate = DateTime.UtcNow.AddYears(-1),
            IsActive = true
        };

        var premium = new PremiumBreakdown
        {
            BasePremium = 5500m,
            AnnualPremium = 5830.40m,
            MonthlyPremium = 485.87m,
            MinimumPremium = 500m
        };

        _rateTableServiceMock
            .Setup(x => x.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProductType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rateEntry);

        _riskCalculatorMock
            .Setup(x => x.CalculatePremium(
                It.IsAny<QuoteRequest>(),
                It.IsAny<RiskAssessment>(),
                It.IsAny<RateTableEntry>()))
            .Returns(premium);

        // Act
        var result = await _sut.Estimate(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PremiumEstimateResponse>(okResult.Value);
        Assert.Equal(5830.40m, response.EstimatedAnnualPremium);
        Assert.Equal(485.87m, response.EstimatedMonthlyPremium);
        Assert.Equal(5500m, response.BasePremium);
        Assert.Equal(0.0328m, response.StateTaxRate);
        Assert.Contains("estimate", response.Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Estimate_NoRateFound_ReturnsEmptyEstimateWithNote()
    {
        // Arrange
        var request = new PremiumEstimateRequest
        {
            ProductType = ProductType.GeneralLiability,
            StateCode = "ZZ",
            CoverageLimit = 1000000m,
            Deductible = 1000m
        };

        _rateTableServiceMock
            .Setup(x => x.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProductType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateTableEntry?)null);

        // Act
        var result = await _sut.Estimate(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PremiumEstimateResponse>(okResult.Value);
        Assert.Equal(0m, response.EstimatedAnnualPremium);
        Assert.Contains("No rate", response.Note);
    }

    [Fact]
    public async Task Estimate_UsesNeutralRiskAssessment()
    {
        // Arrange
        var request = new PremiumEstimateRequest
        {
            ProductType = ProductType.WorkersCompensation,
            StateCode = "CA",
            ClassificationCode = "8810",
            AnnualPayroll = 500000m,
            EmployeeCount = 10,
            CoverageLimit = 1000000m,
            Deductible = 1000m
        };

        var rateEntry = new RateTableEntry
        {
            BaseRate = 2.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m,
            IsActive = true
        };

        _rateTableServiceMock
            .Setup(x => x.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProductType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rateEntry);

        RiskAssessment? capturedRisk = null;
        _riskCalculatorMock
            .Setup(x => x.CalculatePremium(
                It.IsAny<QuoteRequest>(),
                It.IsAny<RiskAssessment>(),
                It.IsAny<RateTableEntry>()))
            .Callback<QuoteRequest, RiskAssessment, RateTableEntry>((_, risk, _) => capturedRisk = risk)
            .Returns(new PremiumBreakdown());

        // Act
        await _sut.Estimate(request, CancellationToken.None);

        // Assert - Neutral risk: score 50, Standard tier
        Assert.NotNull(capturedRisk);
        Assert.Equal(50, capturedRisk.RiskScore);
        Assert.Equal(RiskTier.Standard, capturedRisk.RiskTier);
    }
}
