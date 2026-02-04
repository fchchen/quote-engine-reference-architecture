using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuoteEngine.Api.Controllers;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Controllers;

/// <summary>
/// Unit tests for QuoteController.
/// Tests HTTP response codes and controller behavior.
/// </summary>
public class QuoteControllerTests
{
    private readonly Mock<IQuoteService> _quoteServiceMock;
    private readonly Mock<ILogger<QuoteController>> _loggerMock;
    private readonly QuoteController _sut;

    public QuoteControllerTests()
    {
        _quoteServiceMock = new Mock<IQuoteService>();
        _loggerMock = new Mock<ILogger<QuoteController>>();
        _sut = new QuoteController(_quoteServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateQuote_ValidRequest_ReturnsOkWithQuote()
    {
        // Arrange
        var request = CreateValidQuoteRequest();
        var expectedResponse = CreateQuoteResponse();

        _quoteServiceMock
            .Setup(x => x.CalculateQuoteAsync(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateQuote(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<QuoteResponse>(okResult.Value);
        Assert.Equal(expectedResponse.QuoteNumber, response.QuoteNumber);
    }

    [Fact]
    public async Task CreateQuote_ServiceReturnsNull_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidQuoteRequest();

        _quoteServiceMock
            .Setup(x => x.CalculateQuoteAsync(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuoteResponse?)null);

        // Act
        var result = await _sut.CreateQuote(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<ProblemDetails>(badRequestResult.Value);
    }

    [Fact]
    public async Task GetQuote_ExistingQuote_ReturnsOk()
    {
        // Arrange
        var quoteNumber = "QT-20240101-12345";
        var expectedResponse = CreateQuoteResponse();

        _quoteServiceMock
            .Setup(x => x.GetQuoteAsync(quoteNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetQuote(quoteNumber, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<QuoteResponse>(okResult.Value);
    }

    [Fact]
    public async Task GetQuote_NonExistentQuote_ReturnsNotFound()
    {
        // Arrange
        var quoteNumber = "QT-NONEXISTENT";

        _quoteServiceMock
            .Setup(x => x.GetQuoteAsync(quoteNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuoteResponse?)null);

        // Act
        var result = await _sut.GetQuote(quoteNumber, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.IsType<ProblemDetails>(notFoundResult.Value);
    }

    [Fact]
    public async Task CheckEligibility_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = CreateValidQuoteRequest();
        var expectedResult = new EligibilityResult
        {
            IsEligible = true,
            Messages = new List<string>()
        };

        _quoteServiceMock
            .Setup(x => x.CheckEligibilityAsync(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.CheckEligibility(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var eligibility = Assert.IsType<EligibilityResult>(okResult.Value);
        Assert.True(eligibility.IsEligible);
    }

    [Fact]
    public async Task GetQuoteHistory_ReturnsOk()
    {
        // Arrange
        var taxId = "12-3456789";
        var expectedHistory = new List<QuoteResponse> { CreateQuoteResponse() };

        _quoteServiceMock
            .Setup(x => x.GetQuoteHistoryAsync(taxId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _sut.GetQuoteHistory(taxId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsAssignableFrom<IEnumerable<QuoteResponse>>(okResult.Value);
        Assert.Single(history);
    }

    #region Helper Methods

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

    private static QuoteResponse CreateQuoteResponse()
    {
        return new QuoteResponse
        {
            QuoteNumber = "QT-20240101-12345",
            QuoteDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            Status = QuoteStatus.Quoted,
            BusinessName = "Test Business LLC",
            BusinessType = BusinessType.Technology,
            ProductType = ProductType.GeneralLiability,
            StateCode = "CA",
            CoverageLimit = 1000000m,
            Deductible = 1000m,
            EffectiveDate = DateTime.UtcNow.AddDays(1),
            PolicyExpirationDate = DateTime.UtcNow.AddDays(1).AddYears(1),
            Premium = new PremiumBreakdown
            {
                BasePremium = 5500m,
                AnnualPremium = 5830.40m,
                MonthlyPremium = 485.87m
            },
            RiskAssessment = new RiskAssessment
            {
                RiskScore = 45,
                RiskTier = RiskTier.Standard
            },
            IsEligible = true
        };
    }

    #endregion
}
