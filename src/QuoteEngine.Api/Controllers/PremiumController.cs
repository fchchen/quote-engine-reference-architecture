using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteEngine.Api.Models;
using QuoteEngine.Api.Services;

namespace QuoteEngine.Api.Controllers;

/// <summary>
/// Controller for premium estimation without a full quote.
/// Provides real-time premium updates for the UI.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PremiumController : ControllerBase
{
    private readonly IRateTableService _rateTableService;
    private readonly IRiskCalculator _riskCalculator;
    private readonly ILogger<PremiumController> _logger;

    public PremiumController(
        IRateTableService rateTableService,
        IRiskCalculator riskCalculator,
        ILogger<PremiumController> logger)
    {
        _rateTableService = rateTableService ?? throw new ArgumentNullException(nameof(rateTableService));
        _riskCalculator = riskCalculator ?? throw new ArgumentNullException(nameof(riskCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get a premium estimate without generating a full quote.
    /// Uses neutral risk assessment for quick estimates.
    /// </summary>
    [HttpPost("estimate")]
    [ProducesResponseType(typeof(PremiumEstimateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PremiumEstimateResponse>> Estimate(
        [FromBody] PremiumEstimateRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Premium estimate requested for Product: {ProductType}, State: {StateCode}",
            request.ProductType,
            request.StateCode);

        var classificationCode = request.ClassificationCode ?? "DEFAULT";

        var rateEntry = await _rateTableService
            .GetRateAsync(request.StateCode, classificationCode, request.ProductType, cancellationToken)
            .ConfigureAwait(false);

        if (rateEntry is null)
        {
            return Ok(new PremiumEstimateResponse
            {
                Note = "No rate available for the requested coverage."
            });
        }

        // Build a minimal QuoteRequest for the calculator
        var quoteRequest = new QuoteRequest
        {
            BusinessName = "Estimate",
            TaxId = "00-0000000",
            BusinessType = BusinessType.Office,
            StateCode = request.StateCode,
            ClassificationCode = classificationCode,
            ProductType = request.ProductType,
            AnnualPayroll = request.AnnualPayroll,
            AnnualRevenue = request.AnnualRevenue,
            EmployeeCount = request.EmployeeCount > 0 ? request.EmployeeCount : 1,
            YearsInBusiness = 5,
            CoverageLimit = request.CoverageLimit,
            Deductible = request.Deductible
        };

        // Neutral risk assessment for estimates (same approach as Azure Functions CalculatePremium)
        var neutralRisk = new RiskAssessment
        {
            RiskScore = 50,
            RiskTier = RiskTier.Standard,
            FactorScores = new List<RiskFactorScore>()
        };

        var premium = _riskCalculator.CalculatePremium(quoteRequest, neutralRisk, rateEntry);

        var response = new PremiumEstimateResponse
        {
            EstimatedAnnualPremium = premium.AnnualPremium,
            EstimatedMonthlyPremium = premium.MonthlyPremium,
            BasePremium = premium.BasePremium,
            MinimumPremium = premium.MinimumPremium,
            StateTaxRate = rateEntry.StateTaxRate,
            Note = "This is an estimate. Final premium may vary based on full underwriting."
        };

        return Ok(response);
    }
}
