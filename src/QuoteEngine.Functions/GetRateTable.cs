using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace QuoteEngine.Functions;

/// <summary>
/// Azure Function for rate table and reference data lookups.
/// </summary>
public class GetRateTable
{
    private readonly ILogger<GetRateTable> _logger;

    public GetRateTable(ILogger<GetRateTable> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get available states for quoting.
    /// GET /api/reference/states
    /// </summary>
    [Function("GetStates")]
    public IActionResult GetStates(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "reference/states")] HttpRequest req)
    {
        _logger.LogInformation("Getting available states");

        var states = new[]
        {
            new { code = "CA", name = "California" },
            new { code = "TX", name = "Texas" },
            new { code = "NY", name = "New York" },
            new { code = "FL", name = "Florida" },
            new { code = "IL", name = "Illinois" },
            new { code = "PA", name = "Pennsylvania" },
            new { code = "OH", name = "Ohio" },
            new { code = "GA", name = "Georgia" },
            new { code = "NC", name = "North Carolina" },
            new { code = "MI", name = "Michigan" }
        };

        return new OkObjectResult(states);
    }

    /// <summary>
    /// Get available product types.
    /// GET /api/reference/products
    /// </summary>
    [Function("GetProducts")]
    public IActionResult GetProducts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "reference/products")] HttpRequest req)
    {
        _logger.LogInformation("Getting available products");

        var products = new[]
        {
            new { type = "WorkersCompensation", name = "Workers' Compensation", description = "Coverage for employee injuries" },
            new { type = "GeneralLiability", name = "General Liability", description = "Coverage for third-party claims" },
            new { type = "BusinessOwnersPolicy", name = "Business Owners Policy (BOP)", description = "Combined property and liability" },
            new { type = "CommercialAuto", name = "Commercial Auto", description = "Coverage for business vehicles" },
            new { type = "ProfessionalLiability", name = "Professional Liability (E&O)", description = "Errors and omissions coverage" },
            new { type = "CyberLiability", name = "Cyber Liability", description = "Data breach and cyber attack coverage" }
        };

        return new OkObjectResult(products);
    }

    /// <summary>
    /// Get business types.
    /// GET /api/reference/business-types
    /// </summary>
    [Function("GetBusinessTypes")]
    public IActionResult GetBusinessTypes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "reference/business-types")] HttpRequest req)
    {
        _logger.LogInformation("Getting business types");

        var types = new[]
        {
            new { type = "Retail", name = "Retail", description = "Stores selling goods directly to consumers" },
            new { type = "Restaurant", name = "Restaurant", description = "Food service establishments" },
            new { type = "Office", name = "Office", description = "Professional office environments" },
            new { type = "Manufacturing", name = "Manufacturing", description = "Production and assembly operations" },
            new { type = "Construction", name = "Construction", description = "Building and construction trades" },
            new { type = "Technology", name = "Technology", description = "Software and IT services" },
            new { type = "Healthcare", name = "Healthcare", description = "Medical and health services" },
            new { type = "Transportation", name = "Transportation", description = "Trucking and delivery services" },
            new { type = "RealEstate", name = "Real Estate", description = "Property management and sales" },
            new { type = "ProfessionalServices", name = "Professional Services", description = "Consulting and professional services" }
        };

        return new OkObjectResult(types);
    }

    /// <summary>
    /// Get classification codes for a product type.
    /// GET /api/reference/classifications/{productType}
    /// </summary>
    [Function("GetClassifications")]
    public IActionResult GetClassifications(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "reference/classifications/{productType}")] HttpRequest req,
        string productType)
    {
        _logger.LogInformation("Getting classifications for {ProductType}", productType);

        var classifications = productType switch
        {
            "WorkersCompensation" => new[]
            {
                new { code = "8810", description = "Clerical Office Employees" },
                new { code = "8742", description = "Salespersons - Outside" },
                new { code = "8820", description = "Attorneys - All Employees" },
                new { code = "8832", description = "Physicians - All Employees" },
                new { code = "5183", description = "Plumbing - Residential" },
                new { code = "5190", description = "Electrical Work" },
                new { code = "5403", description = "Carpentry - Residential" }
            },
            "GeneralLiability" => new[]
            {
                new { code = "41677", description = "Restaurant - No Liquor" },
                new { code = "41675", description = "Restaurant - With Liquor" },
                new { code = "91111", description = "Retail Store" },
                new { code = "41650", description = "Office - Professional" },
                new { code = "91302", description = "Contractor - General" }
            },
            _ => new[]
            {
                new { code = "DEFAULT", description = "Standard Classification" }
            }
        };

        return new OkObjectResult(classifications);
    }

    /// <summary>
    /// Health check endpoint.
    /// GET /api/health
    /// </summary>
    [Function("HealthCheck")]
    public IActionResult HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        return new OkObjectResult(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
