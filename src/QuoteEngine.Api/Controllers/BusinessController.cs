using Microsoft.AspNetCore.Mvc;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;

namespace QuoteEngine.Api.Controllers;

/// <summary>
/// API controller for business lookup operations.
/// Supports the business search/autocomplete functionality in the frontend.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BusinessController : ControllerBase
{
    private readonly IBusinessLookupService _businessLookupService;
    private readonly ILogger<BusinessController> _logger;

    public BusinessController(
        IBusinessLookupService businessLookupService,
        ILogger<BusinessController> logger)
    {
        _businessLookupService = businessLookupService ?? throw new ArgumentNullException(nameof(businessLookupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search for businesses with optional filtering.
    /// </summary>
    /// <param name="searchTerm">Search term for name/DBA/TaxId</param>
    /// <param name="stateCode">Filter by state</param>
    /// <param name="businessType">Filter by business type</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of businesses</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(BusinessSearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BusinessSearchResponse>> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] string? stateCode,
        [FromQuery] BusinessType? businessType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Enforce page size limits
        pageSize = Math.Clamp(pageSize, 1, 50);
        pageNumber = Math.Max(1, pageNumber);

        _logger.LogDebug(
            "Business search: Term={Term}, State={State}, Type={Type}",
            searchTerm, stateCode, businessType);

        var request = new BusinessSearchRequest
        {
            SearchTerm = searchTerm,
            StateCode = stateCode,
            BusinessType = businessType,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _businessLookupService.SearchAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get a business by ID.
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business information</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BusinessInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessInfo>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var business = await _businessLookupService.GetByIdAsync(id, cancellationToken);

        if (business is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Business not found",
                Detail = $"No business found with ID {id}",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(business);
    }

    /// <summary>
    /// Get a business by Tax ID.
    /// </summary>
    /// <param name="taxId">Business Tax ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business information</returns>
    [HttpGet("taxid/{taxId}")]
    [ProducesResponseType(typeof(BusinessInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessInfo>> GetByTaxId(
        [FromRoute] string taxId,
        CancellationToken cancellationToken)
    {
        var business = await _businessLookupService.GetByTaxIdAsync(taxId, cancellationToken);

        if (business is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Business not found",
                Detail = $"No business found with Tax ID '{taxId}'",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(business);
    }
}
