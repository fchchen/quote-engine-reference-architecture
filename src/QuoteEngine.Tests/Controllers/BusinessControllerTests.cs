using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuoteEngine.Api.Controllers;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Controllers;

/// <summary>
/// Unit tests for BusinessController.
/// Tests HTTP response codes and controller behavior for business lookup operations.
/// </summary>
public class BusinessControllerTests
{
    private readonly Mock<IBusinessLookupService> _businessLookupServiceMock;
    private readonly Mock<ILogger<BusinessController>> _loggerMock;
    private readonly BusinessController _sut;

    public BusinessControllerTests()
    {
        _businessLookupServiceMock = new Mock<IBusinessLookupService>();
        _loggerMock = new Mock<ILogger<BusinessController>>();
        _sut = new BusinessController(_businessLookupServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Search_WithSearchTerm_ReturnsOkWithResults()
    {
        // Arrange
        var expectedResponse = new BusinessSearchResponse
        {
            Businesses = new List<BusinessInfo>
            {
                new() { Id = 1, BusinessName = "Pacific Coast Brewing Co", TaxId = "11-2233445", StateCode = "WA" }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _businessLookupServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<BusinessSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Search("Pacific", null, null, 1, 10, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BusinessSearchResponse>(okResult.Value);
        Assert.Single(response.Businesses);
        Assert.Equal("Pacific Coast Brewing Co", response.Businesses[0].BusinessName);
    }

    [Fact]
    public async Task Search_NoResults_ReturnsOkWithEmptyList()
    {
        // Arrange
        var expectedResponse = new BusinessSearchResponse
        {
            Businesses = new List<BusinessInfo>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _businessLookupServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<BusinessSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Search("NonexistentBusiness", null, null, 1, 10, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BusinessSearchResponse>(okResult.Value);
        Assert.Empty(response.Businesses);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public async Task Search_ClampsPageSize_To50Max()
    {
        // Arrange
        BusinessSearchRequest? capturedRequest = null;
        _businessLookupServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<BusinessSearchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<BusinessSearchRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new BusinessSearchResponse());

        // Act
        await _sut.Search("test", null, null, 1, 100, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(50, capturedRequest.PageSize);
    }

    [Fact]
    public async Task Search_ClampsPageNumber_To1Min()
    {
        // Arrange
        BusinessSearchRequest? capturedRequest = null;
        _businessLookupServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<BusinessSearchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<BusinessSearchRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new BusinessSearchResponse());

        // Act
        await _sut.Search("test", null, null, -1, 10, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(1, capturedRequest.PageNumber);
    }

    [Fact]
    public async Task Search_PassesFiltersToService()
    {
        // Arrange
        BusinessSearchRequest? capturedRequest = null;
        _businessLookupServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<BusinessSearchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<BusinessSearchRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new BusinessSearchResponse());

        // Act
        await _sut.Search("test", "CA", BusinessType.Technology, 2, 5, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("test", capturedRequest.SearchTerm);
        Assert.Equal("CA", capturedRequest.StateCode);
        Assert.Equal(BusinessType.Technology, capturedRequest.BusinessType);
        Assert.Equal(2, capturedRequest.PageNumber);
        Assert.Equal(5, capturedRequest.PageSize);
    }

    [Fact]
    public async Task GetById_ExistingBusiness_ReturnsOk()
    {
        // Arrange
        var expectedBusiness = new BusinessInfo
        {
            Id = 1,
            BusinessName = "Sunrise Bakery LLC",
            TaxId = "12-3456789",
            StateCode = "CA"
        };

        _businessLookupServiceMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBusiness);

        // Act
        var result = await _sut.GetById(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var business = Assert.IsType<BusinessInfo>(okResult.Value);
        Assert.Equal("Sunrise Bakery LLC", business.BusinessName);
    }

    [Fact]
    public async Task GetById_NonExistentBusiness_ReturnsNotFound()
    {
        // Arrange
        _businessLookupServiceMock
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessInfo?)null);

        // Act
        var result = await _sut.GetById(999, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Contains("999", problemDetails.Detail);
    }

    [Fact]
    public async Task GetByTaxId_ExistingBusiness_ReturnsOk()
    {
        // Arrange
        var expectedBusiness = new BusinessInfo
        {
            Id = 1,
            BusinessName = "Sunrise Bakery LLC",
            TaxId = "12-3456789",
            StateCode = "CA"
        };

        _businessLookupServiceMock
            .Setup(x => x.GetByTaxIdAsync("12-3456789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBusiness);

        // Act
        var result = await _sut.GetByTaxId("12-3456789", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var business = Assert.IsType<BusinessInfo>(okResult.Value);
        Assert.Equal("12-3456789", business.TaxId);
    }

    [Fact]
    public async Task GetByTaxId_NonExistentBusiness_ReturnsNotFound()
    {
        // Arrange
        _businessLookupServiceMock
            .Setup(x => x.GetByTaxIdAsync("99-9999999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessInfo?)null);

        // Act
        var result = await _sut.GetByTaxId("99-9999999", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Contains("99-9999999", problemDetails.Detail);
    }
}
