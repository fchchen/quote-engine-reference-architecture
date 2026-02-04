using Microsoft.AspNetCore.Mvc;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;

namespace QuoteEngine.Api.Controllers;

/// <summary>
/// RESTful API controller for quote operations.
///
/// INTERVIEW TALKING POINTS:
/// - Inherits from ControllerBase (not Controller) - no view support needed for API
/// - Uses [ApiController] attribute for automatic model validation and binding
/// - Constructor injection for dependencies
/// - Async/await for all operations
/// - Proper HTTP verb usage and status codes
/// - CancellationToken support for cancellable operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class QuoteController : ControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly ILogger<QuoteController> _logger;

    /// <summary>
    /// Constructor with dependency injection.
    /// INTERVIEW TIP: Use constructor injection for required dependencies.
    /// </summary>
    public QuoteController(IQuoteService quoteService, ILogger<QuoteController> logger)
    {
        _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new insurance quote.
    /// </summary>
    /// <param name="request">Quote request with business and coverage details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quote response with premium breakdown</returns>
    /// <response code="200">Quote calculated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuoteResponse>> CreateQuote(
        [FromBody] QuoteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Quote request received for {BusinessName}, Product: {ProductType}",
            request.BusinessName,
            request.ProductType);

        // Model validation is automatic with [ApiController]
        // Invalid requests return 400 with validation errors

        var result = await _quoteService.CalculateQuoteAsync(request, cancellationToken);

        if (result is null)
        {
            _logger.LogWarning("Quote calculation returned null for {BusinessName}", request.BusinessName);
            return BadRequest(new ProblemDetails
            {
                Title = "Quote calculation failed",
                Detail = "Unable to generate a quote for the provided information",
                Status = StatusCodes.Status400BadRequest
            });
        }

        _logger.LogInformation(
            "Quote {QuoteNumber} created with premium {Premium:C}",
            result.QuoteNumber,
            result.Premium.AnnualPremium);

        return Ok(result);
    }

    /// <summary>
    /// Retrieve an existing quote by quote number.
    /// </summary>
    /// <param name="quoteNumber">Unique quote identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quote response if found</returns>
    /// <response code="200">Quote found</response>
    /// <response code="404">Quote not found</response>
    [HttpGet("{quoteNumber}")]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuoteResponse>> GetQuote(
        [FromRoute] string quoteNumber,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving quote {QuoteNumber}", quoteNumber);

        var quote = await _quoteService.GetQuoteAsync(quoteNumber, cancellationToken);

        if (quote is null)
        {
            _logger.LogWarning("Quote {QuoteNumber} not found", quoteNumber);
            return NotFound(new ProblemDetails
            {
                Title = "Quote not found",
                Detail = $"No quote found with number '{quoteNumber}'",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(quote);
    }

    /// <summary>
    /// Check eligibility for a quote without generating a full quote.
    /// Useful for pre-validation before collecting all information.
    /// </summary>
    /// <param name="request">Partial quote request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Eligibility result</returns>
    [HttpPost("eligibility")]
    [ProducesResponseType(typeof(EligibilityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EligibilityResult>> CheckEligibility(
        [FromBody] QuoteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking eligibility for {BusinessName}", request.BusinessName);

        var result = await _quoteService.CheckEligibilityAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get quote history for a business.
    /// </summary>
    /// <param name="taxId">Business tax ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of historical quotes</returns>
    [HttpGet("history/{taxId}")]
    [ProducesResponseType(typeof(IEnumerable<QuoteResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<QuoteResponse>>> GetQuoteHistory(
        [FromRoute] string taxId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving quote history for TaxId: {TaxId}", taxId);

        var history = await _quoteService.GetQuoteHistoryAsync(taxId, cancellationToken);

        return Ok(history);
    }
}
