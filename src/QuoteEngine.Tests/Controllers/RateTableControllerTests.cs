using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuoteEngine.Api.Controllers;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;
using Xunit;

namespace QuoteEngine.Tests.Controllers;

/// <summary>
/// Unit tests for RateTableController.
/// Tests HTTP response codes and reference data endpoints.
/// </summary>
public class RateTableControllerTests
{
    private readonly Mock<IRateTableService> _rateTableServiceMock;
    private readonly Mock<ILogger<RateTableController>> _loggerMock;
    private readonly RateTableController _sut;

    public RateTableControllerTests()
    {
        _rateTableServiceMock = new Mock<IRateTableService>();
        _loggerMock = new Mock<ILogger<RateTableController>>();
        _sut = new RateTableController(_rateTableServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetClassificationCodes_ReturnsOkWithCodes()
    {
        // Arrange
        var expectedCodes = new List<ClassificationCode>
        {
            new() { Code = "8810", Description = "Clerical Office Employees", ProductType = ProductType.WorkersCompensation },
            new() { Code = "5183", Description = "Plumbing - Residential", ProductType = ProductType.WorkersCompensation }
        };

        _rateTableServiceMock
            .Setup(x => x.GetClassificationCodesAsync(ProductType.WorkersCompensation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCodes);

        // Act
        var result = await _sut.GetClassificationCodes(ProductType.WorkersCompensation, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var codes = Assert.IsAssignableFrom<IEnumerable<ClassificationCode>>(okResult.Value);
        Assert.Equal(2, codes.Count());
    }

    [Fact]
    public async Task GetRate_ExistingRate_ReturnsOk()
    {
        // Arrange
        var expectedRate = new RateTableEntry
        {
            Id = 1,
            StateCode = "CA",
            ClassificationCode = "8810",
            ProductType = ProductType.WorkersCompensation,
            BaseRate = 2.50m,
            MinPremium = 1000m,
            StateTaxRate = 0.0328m
        };

        _rateTableServiceMock
            .Setup(x => x.GetRateAsync("CA", "8810", ProductType.WorkersCompensation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRate);

        // Act
        var result = await _sut.GetRate("CA", "8810", ProductType.WorkersCompensation, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var rate = Assert.IsType<RateTableEntry>(okResult.Value);
        Assert.Equal(2.50m, rate.BaseRate);
        Assert.Equal("CA", rate.StateCode);
    }

    [Fact]
    public async Task GetRate_NonExistentRate_ReturnsNotFound()
    {
        // Arrange
        _rateTableServiceMock
            .Setup(x => x.GetRateAsync("ZZ", "9999", ProductType.WorkersCompensation, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateTableEntry?)null);

        // Act
        var result = await _sut.GetRate("ZZ", "9999", ProductType.WorkersCompensation, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.IsType<ProblemDetails>(notFoundResult.Value);
    }

    [Fact]
    public void GetStates_ReturnsListOfStates()
    {
        // Act
        var result = _sut.GetStates();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var states = Assert.IsAssignableFrom<IEnumerable<StateInfo>>(okResult.Value);
        var stateList = states.ToList();
        Assert.True(stateList.Count >= 10);
        Assert.Contains(stateList, s => s.Code == "CA" && s.Name == "California");
        Assert.Contains(stateList, s => s.Code == "TX" && s.Name == "Texas");
        Assert.Contains(stateList, s => s.Code == "NY" && s.Name == "New York");
    }

    [Fact]
    public void GetProducts_ReturnsSixProductTypes()
    {
        // Act
        var result = _sut.GetProducts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IEnumerable<ProductInfo>>(okResult.Value);
        var productList = products.ToList();
        Assert.Equal(6, productList.Count);
        Assert.Contains(productList, p => p.Type == ProductType.WorkersCompensation);
        Assert.Contains(productList, p => p.Type == ProductType.GeneralLiability);
        Assert.Contains(productList, p => p.Type == ProductType.BusinessOwnersPolicy);
        Assert.Contains(productList, p => p.Type == ProductType.CommercialAuto);
        Assert.Contains(productList, p => p.Type == ProductType.ProfessionalLiability);
        Assert.Contains(productList, p => p.Type == ProductType.CyberLiability);
    }

    [Fact]
    public void GetProducts_EachProductHasNameAndDescription()
    {
        // Act
        var result = _sut.GetProducts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IEnumerable<ProductInfo>>(okResult.Value);
        foreach (var product in products)
        {
            Assert.False(string.IsNullOrEmpty(product.Name));
            Assert.False(string.IsNullOrEmpty(product.Description));
        }
    }

    [Fact]
    public void GetBusinessTypes_ReturnsAllBusinessTypes()
    {
        // Act
        var result = _sut.GetBusinessTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var types = Assert.IsAssignableFrom<IEnumerable<BusinessTypeInfo>>(okResult.Value);
        var typeList = types.ToList();
        var expectedCount = Enum.GetValues<BusinessType>().Length;
        Assert.Equal(expectedCount, typeList.Count);
    }

    [Fact]
    public void GetBusinessTypes_EachTypeHasFormattedNameAndDescription()
    {
        // Act
        var result = _sut.GetBusinessTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var types = Assert.IsAssignableFrom<IEnumerable<BusinessTypeInfo>>(okResult.Value);
        foreach (var type in types)
        {
            Assert.False(string.IsNullOrEmpty(type.Name));
            Assert.False(string.IsNullOrEmpty(type.Description));
        }
    }

    [Fact]
    public void GetBusinessTypes_FormatsEnumNames_WithSpaces()
    {
        // Act
        var result = _sut.GetBusinessTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var types = Assert.IsAssignableFrom<IEnumerable<BusinessTypeInfo>>(okResult.Value);
        var typeList = types.ToList();

        // ProfessionalServices should be "Professional Services"
        var profServices = typeList.First(t => t.Type == BusinessType.ProfessionalServices);
        Assert.Equal("Professional Services", profServices.Name);

        // RealEstate should be "Real Estate"
        var realEstate = typeList.First(t => t.Type == BusinessType.RealEstate);
        Assert.Equal("Real Estate", realEstate.Name);
    }
}
