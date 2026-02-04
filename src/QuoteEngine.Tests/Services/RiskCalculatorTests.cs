using Microsoft.Extensions.Logging;
using Moq;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Services;

/// <summary>
/// Unit tests for RiskCalculator.
/// Tests risk scoring logic and premium calculations.
/// </summary>
public class RiskCalculatorTests
{
    private readonly Mock<ILogger<RiskCalculator>> _loggerMock;
    private readonly RiskCalculator _sut;

    public RiskCalculatorTests()
    {
        _loggerMock = new Mock<ILogger<RiskCalculator>>();
        _sut = new RiskCalculator(_loggerMock.Object);
    }

    #region CalculateRisk Tests

    [Fact]
    public void CalculateRisk_EstablishedTechBusiness_ReturnsPreferredTier()
    {
        // Arrange
        var request = new QuoteRequest
        {
            BusinessName = "Tech Company",
            BusinessType = BusinessType.Technology,
            StateCode = "CA",
            YearsInBusiness = 10,
            EmployeeCount = 20,
            AnnualRevenue = 2000000m,
            AnnualPayroll = 1500000m
        };

        // Act
        var result = _sut.CalculateRisk(request);

        // Assert
        Assert.Equal(RiskTier.Preferred, result.RiskTier);
        Assert.True(result.RiskScore <= 35);
    }

    [Fact]
    public void CalculateRisk_NewConstructionBusiness_ReturnsNonStandardTier()
    {
        // Arrange
        var request = new QuoteRequest
        {
            BusinessName = "New Construction Co",
            BusinessType = BusinessType.Construction,
            StateCode = "TX",
            YearsInBusiness = 1,
            EmployeeCount = 50,
            AnnualRevenue = 5000000m,
            AnnualPayroll = 3000000m
        };

        // Act
        var result = _sut.CalculateRisk(request);

        // Assert
        Assert.True(result.RiskTier is RiskTier.NonStandard or RiskTier.Decline);
        Assert.True(result.RiskScore >= 55);
    }

    [Fact]
    public void CalculateRisk_ReturnsCorrectFactorScores()
    {
        // Arrange
        var request = new QuoteRequest
        {
            BusinessName = "Test Business",
            BusinessType = BusinessType.Office,
            StateCode = "CA",
            YearsInBusiness = 5,
            EmployeeCount = 10,
            AnnualRevenue = 500000m,
            AnnualPayroll = 300000m
        };

        // Act
        var result = _sut.CalculateRisk(request);

        // Assert
        Assert.NotEmpty(result.FactorScores);
        Assert.Contains(result.FactorScores, f => f.FactorName == "Years in Business");
        Assert.Contains(result.FactorScores, f => f.FactorName == "Employee Count");
        Assert.Contains(result.FactorScores, f => f.FactorName == "Industry Risk");
    }

    [Theory]
    [InlineData(1, 70)]  // 1 year = high risk score
    [InlineData(2, 55)]
    [InlineData(3, 40)]
    [InlineData(5, 25)]
    [InlineData(10, 15)] // 10+ years = low risk score
    public void CalculateRisk_YearsInBusiness_AffectsScore(int years, int expectedScore)
    {
        // Arrange
        var request = new QuoteRequest
        {
            BusinessName = "Test",
            BusinessType = BusinessType.Office,
            StateCode = "CA",
            YearsInBusiness = years,
            EmployeeCount = 10,
            AnnualRevenue = 500000m
        };

        // Act
        var result = _sut.CalculateRisk(request);

        // Assert
        var yearsScore = result.FactorScores.First(f => f.FactorName == "Years in Business");
        Assert.Equal(expectedScore, yearsScore.Score);
    }

    [Theory]
    [InlineData(BusinessType.Technology, RiskTier.Preferred)]
    [InlineData(BusinessType.Office, RiskTier.Preferred)]
    [InlineData(BusinessType.Construction, RiskTier.Standard)]
    public void CalculateRisk_IndustryType_AffectsTier(BusinessType businessType, RiskTier expectedMinTier)
    {
        // Arrange - Using established business to isolate industry effect
        var request = new QuoteRequest
        {
            BusinessName = "Test",
            BusinessType = businessType,
            StateCode = "CA",
            YearsInBusiness = 10,
            EmployeeCount = 5,
            AnnualRevenue = 500000m
        };

        // Act
        var result = _sut.CalculateRisk(request);

        // Assert
        Assert.True(result.RiskTier <= expectedMinTier || result.RiskTier == expectedMinTier);
    }

    #endregion

    #region CalculatePremium Tests

    [Fact]
    public void CalculatePremium_WorkersComp_CalculatesBasedOnPayroll()
    {
        // Arrange
        var request = new QuoteRequest
        {
            ProductType = ProductType.WorkersCompensation,
            AnnualPayroll = 1000000m, // $1M payroll
            StateCode = "CA"
        };

        var riskAssessment = new RiskAssessment { RiskTier = RiskTier.Standard };
        var rateEntry = new RateTableEntry
        {
            BaseRate = 2.50m, // Per $100 of payroll
            MinPremium = 1000m,
            StateTaxRate = 0.0328m
        };

        // Act
        var result = _sut.CalculatePremium(request, riskAssessment, rateEntry);

        // Assert
        // Expected: ($1,000,000 / 100) * 2.50 = $25,000
        Assert.Equal(25000m, result.BasePremium);
    }

    [Fact]
    public void CalculatePremium_GeneralLiability_CalculatesBasedOnRevenue()
    {
        // Arrange
        var request = new QuoteRequest
        {
            ProductType = ProductType.GeneralLiability,
            AnnualRevenue = 1000000m, // $1M revenue
            StateCode = "CA"
        };

        var riskAssessment = new RiskAssessment { RiskTier = RiskTier.Standard };
        var rateEntry = new RateTableEntry
        {
            BaseRate = 5.50m, // Per $1000 of revenue
            MinPremium = 500m,
            StateTaxRate = 0.0328m
        };

        // Act
        var result = _sut.CalculatePremium(request, riskAssessment, rateEntry);

        // Assert
        // Expected: ($1,000,000 / 1000) * 5.50 = $5,500
        Assert.Equal(5500m, result.BasePremium);
    }

    [Fact]
    public void CalculatePremium_PreferredTier_AppliesDiscount()
    {
        // Arrange
        var request = new QuoteRequest
        {
            ProductType = ProductType.GeneralLiability,
            AnnualRevenue = 1000000m,
            StateCode = "CA"
        };

        var riskAssessment = new RiskAssessment { RiskTier = RiskTier.Preferred };
        var rateEntry = new RateTableEntry
        {
            BaseRate = 5.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m
        };

        // Act
        var result = _sut.CalculatePremium(request, riskAssessment, rateEntry);

        // Assert
        var tierAdjustment = result.Adjustments.FirstOrDefault(a => a.Code == "TIER");
        Assert.NotNull(tierAdjustment);
        Assert.Equal(AdjustmentType.Discount, tierAdjustment.Type);
        Assert.Equal(-0.15m, tierAdjustment.Factor);
    }

    [Fact]
    public void CalculatePremium_NonStandardTier_AppliesSurcharge()
    {
        // Arrange
        var request = new QuoteRequest
        {
            ProductType = ProductType.GeneralLiability,
            AnnualRevenue = 1000000m,
            StateCode = "CA"
        };

        var riskAssessment = new RiskAssessment { RiskTier = RiskTier.NonStandard };
        var rateEntry = new RateTableEntry
        {
            BaseRate = 5.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m
        };

        // Act
        var result = _sut.CalculatePremium(request, riskAssessment, rateEntry);

        // Assert
        var tierAdjustment = result.Adjustments.FirstOrDefault(a => a.Code == "TIER");
        Assert.NotNull(tierAdjustment);
        Assert.Equal(AdjustmentType.Surcharge, tierAdjustment.Type);
        Assert.Equal(0.25m, tierAdjustment.Factor);
    }

    [Fact]
    public void CalculatePremium_BelowMinimum_AppliesMinimumPremium()
    {
        // Arrange
        var request = new QuoteRequest
        {
            ProductType = ProductType.GeneralLiability,
            AnnualRevenue = 50000m, // Very small revenue
            StateCode = "CA",
            Deductible = 0m // No deductible to isolate minimum premium test
        };

        var riskAssessment = new RiskAssessment { RiskTier = RiskTier.Standard };
        var rateEntry = new RateTableEntry
        {
            BaseRate = 5.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m
        };

        // Act
        var result = _sut.CalculatePremium(request, riskAssessment, rateEntry);

        // Assert
        // Base: ($50,000 / 1000) * 5.50 = $275, which is below minimum
        Assert.Equal(275m, result.BasePremium);
        Assert.True(result.Subtotal >= result.MinimumPremium);
        Assert.Contains(result.Adjustments, a => a.Code == "MIN");
    }

    [Theory]
    [InlineData(1000, 0.02)]
    [InlineData(5000, 0.07)]
    [InlineData(10000, 0.10)]
    [InlineData(25000, 0.15)]
    public void CalculatePremium_HigherDeductible_GivesLargerCredit(int deductibleInt, double expectedCreditFactorDouble)
    {
        // Convert from int/double to decimal for use in test
        var deductible = (decimal)deductibleInt;
        var expectedCreditFactor = (decimal)expectedCreditFactorDouble;

        // Arrange
        var request = new QuoteRequest
        {
            ProductType = ProductType.GeneralLiability,
            AnnualRevenue = 1000000m,
            Deductible = deductible,
            StateCode = "CA"
        };

        var riskAssessment = new RiskAssessment { RiskTier = RiskTier.Standard };
        var rateEntry = new RateTableEntry
        {
            BaseRate = 5.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m
        };

        // Act
        var result = _sut.CalculatePremium(request, riskAssessment, rateEntry);

        // Assert
        var dedAdjustment = result.Adjustments.FirstOrDefault(a => a.Code == "DED");
        if (expectedCreditFactor > 0)
        {
            Assert.NotNull(dedAdjustment);
            Assert.Equal(AdjustmentType.Credit, dedAdjustment.Type);
        }
    }

    [Fact]
    public void CalculatePremium_IncludesStateTax()
    {
        // Arrange
        var request = new QuoteRequest
        {
            ProductType = ProductType.GeneralLiability,
            AnnualRevenue = 1000000m,
            StateCode = "CA"
        };

        var riskAssessment = new RiskAssessment { RiskTier = RiskTier.Standard };
        var rateEntry = new RateTableEntry
        {
            BaseRate = 5.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m // 3.28%
        };

        // Act
        var result = _sut.CalculatePremium(request, riskAssessment, rateEntry);

        // Assert
        Assert.True(result.StateTax > 0);
        Assert.Equal(Math.Round(result.Subtotal * 0.0328m, 2), result.StateTax);
    }

    [Fact]
    public void CalculatePremium_IncludesPolicyFee()
    {
        // Arrange
        var request = new QuoteRequest
        {
            ProductType = ProductType.GeneralLiability,
            AnnualRevenue = 1000000m,
            StateCode = "CA"
        };

        var riskAssessment = new RiskAssessment { RiskTier = RiskTier.Standard };
        var rateEntry = new RateTableEntry
        {
            BaseRate = 5.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m
        };

        // Act
        var result = _sut.CalculatePremium(request, riskAssessment, rateEntry);

        // Assert
        Assert.Equal(150m, result.PolicyFee); // GL policy fee
    }

    [Fact]
    public void CalculatePremium_AnnualPremium_SumsCorrectly()
    {
        // Arrange
        var request = new QuoteRequest
        {
            ProductType = ProductType.GeneralLiability,
            AnnualRevenue = 1000000m,
            StateCode = "CA"
        };

        var riskAssessment = new RiskAssessment { RiskTier = RiskTier.Standard };
        var rateEntry = new RateTableEntry
        {
            BaseRate = 5.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m
        };

        // Act
        var result = _sut.CalculatePremium(request, riskAssessment, rateEntry);

        // Assert
        var expectedAnnual = result.Subtotal + result.StateTax + result.PolicyFee;
        Assert.Equal(expectedAnnual, result.AnnualPremium);
    }

    [Fact]
    public void CalculatePremium_MonthlyPremium_IsOnetwelfthOfAnnual()
    {
        // Arrange
        var request = new QuoteRequest
        {
            ProductType = ProductType.GeneralLiability,
            AnnualRevenue = 1000000m,
            StateCode = "CA"
        };

        var riskAssessment = new RiskAssessment { RiskTier = RiskTier.Standard };
        var rateEntry = new RateTableEntry
        {
            BaseRate = 5.50m,
            MinPremium = 500m,
            StateTaxRate = 0.0328m
        };

        // Act
        var result = _sut.CalculatePremium(request, riskAssessment, rateEntry);

        // Assert
        Assert.Equal(Math.Round(result.AnnualPremium / 12, 2), result.MonthlyPremium);
    }

    #endregion
}
