using Microsoft.AspNetCore.Mvc;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;

namespace QuoteEngine.Api.Controllers;

/// <summary>
/// API controller for rate table and classification code lookups.
/// Supports the dynamic form fields and rate lookups in the frontend.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class RateTableController : ControllerBase
{
    private readonly IRateTableService _rateTableService;
    private readonly ILogger<RateTableController> _logger;

    public RateTableController(
        IRateTableService rateTableService,
        ILogger<RateTableController> logger)
    {
        _rateTableService = rateTableService ?? throw new ArgumentNullException(nameof(rateTableService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get classification codes for a product type.
    /// Used to populate dropdown lists in the quote form.
    /// </summary>
    /// <param name="productType">Product type to get codes for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of classification codes</returns>
    [HttpGet("classifications/{productType}")]
    [ProducesResponseType(typeof(IEnumerable<ClassificationCode>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ClassificationCode>>> GetClassificationCodes(
        [FromRoute] ProductType productType,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting classification codes for product: {ProductType}", productType);

        var codes = await _rateTableService.GetClassificationCodesAsync(productType, cancellationToken);

        return Ok(codes);
    }

    /// <summary>
    /// Get rate entry for specific criteria.
    /// Used for premium estimation.
    /// </summary>
    /// <param name="stateCode">State code</param>
    /// <param name="classificationCode">Classification code</param>
    /// <param name="productType">Product type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate table entry if found</returns>
    [HttpGet("rate")]
    [ProducesResponseType(typeof(RateTableEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RateTableEntry>> GetRate(
        [FromQuery] string stateCode,
        [FromQuery] string classificationCode,
        [FromQuery] ProductType productType,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Getting rate for State={State}, Class={Class}, Product={Product}",
            stateCode, classificationCode, productType);

        var rate = await _rateTableService.GetRateAsync(
            stateCode, classificationCode, productType, cancellationToken);

        if (rate is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Rate not found",
                Detail = $"No rate available for the specified criteria",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(rate);
    }

    /// <summary>
    /// Get available states for quoting.
    /// </summary>
    /// <returns>List of state codes</returns>
    [HttpGet("states")]
    [ProducesResponseType(typeof(IEnumerable<StateInfo>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<StateInfo>> GetStates()
    {
        // Return list of states where quoting is available
        var states = new List<StateInfo>
        {
            new() { Code = "CA", Name = "California" },
            new() { Code = "TX", Name = "Texas" },
            new() { Code = "NY", Name = "New York" },
            new() { Code = "FL", Name = "Florida" },
            new() { Code = "IL", Name = "Illinois" },
            new() { Code = "PA", Name = "Pennsylvania" },
            new() { Code = "OH", Name = "Ohio" },
            new() { Code = "GA", Name = "Georgia" },
            new() { Code = "NC", Name = "North Carolina" },
            new() { Code = "MI", Name = "Michigan" }
        };

        return Ok(states);
    }

    /// <summary>
    /// Get available product types.
    /// </summary>
    /// <returns>List of product types with descriptions</returns>
    [HttpGet("products")]
    [ProducesResponseType(typeof(IEnumerable<ProductInfo>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ProductInfo>> GetProducts()
    {
        var products = new List<ProductInfo>
        {
            new() { Type = ProductType.WorkersCompensation, Name = "Workers' Compensation", Description = "Coverage for employee injuries and occupational illnesses" },
            new() { Type = ProductType.GeneralLiability, Name = "General Liability", Description = "Coverage for bodily injury, property damage, and personal injury claims" },
            new() { Type = ProductType.BusinessOwnersPolicy, Name = "Business Owners Policy (BOP)", Description = "Combined property and liability coverage for small businesses" },
            new() { Type = ProductType.CommercialAuto, Name = "Commercial Auto", Description = "Coverage for business-owned vehicles" },
            new() { Type = ProductType.ProfessionalLiability, Name = "Professional Liability (E&O)", Description = "Coverage for errors and omissions in professional services" },
            new() { Type = ProductType.CyberLiability, Name = "Cyber Liability", Description = "Coverage for data breaches and cyber attacks" }
        };

        return Ok(products);
    }

    /// <summary>
    /// Get business types for selection.
    /// </summary>
    /// <returns>List of business types</returns>
    [HttpGet("business-types")]
    [ProducesResponseType(typeof(IEnumerable<BusinessTypeInfo>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<BusinessTypeInfo>> GetBusinessTypes()
    {
        var types = Enum.GetValues<BusinessType>()
            .Select(t => new BusinessTypeInfo
            {
                Type = t,
                Name = FormatEnumName(t.ToString()),
                Description = GetBusinessTypeDescription(t)
            })
            .ToList();

        return Ok(types);
    }

    private static string FormatEnumName(string enumValue)
    {
        // Insert spaces before uppercase letters
        return string.Concat(enumValue.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    private static string GetBusinessTypeDescription(BusinessType type)
    {
        return type switch
        {
            BusinessType.Retail => "Stores selling goods directly to consumers",
            BusinessType.Restaurant => "Food service establishments",
            BusinessType.Office => "Professional office environments",
            BusinessType.Manufacturing => "Production and assembly operations",
            BusinessType.Construction => "Building and construction trades",
            BusinessType.Technology => "Software and IT services",
            BusinessType.Healthcare => "Medical and health services",
            BusinessType.Transportation => "Trucking and delivery services",
            BusinessType.RealEstate => "Property management and sales",
            BusinessType.ProfessionalServices => "Consulting and professional services",
            _ => "General business operations"
        };
    }
}

/// <summary>
/// State information model
/// </summary>
public class StateInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Product information model
/// </summary>
public class ProductInfo
{
    public ProductType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Business type information model
/// </summary>
public class BusinessTypeInfo
{
    public BusinessType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
