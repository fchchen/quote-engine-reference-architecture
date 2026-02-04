using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using QuoteEngine.Functions.Models;
using QuoteEngine.Functions.Services;

namespace QuoteEngine.Functions;

/// <summary>
/// Azure Function for premium estimation without full quote.
/// Useful for real-time premium updates in the UI.
/// </summary>
public class CalculatePremium
{
    private readonly ILogger<CalculatePremium> _logger;
    private readonly MockDataService _dataService;

    public CalculatePremium(ILogger<CalculatePremium> logger)
    {
        _logger = logger;
        _dataService = new MockDataService();
    }

    /// <summary>
    /// HTTP trigger function to calculate premium estimate.
    /// POST /api/premium/estimate
    /// </summary>
    [Function("CalculatePremium")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "premium/estimate")] HttpRequest req)
    {
        _logger.LogInformation("Premium estimate request received");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<PremiumEstimateRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request is null)
            {
                return new BadRequestObjectResult(new { error = "Invalid request body" });
            }

            // Get rate
            var rate = _dataService.GetRate(request.StateCode, request.ClassificationCode ?? "DEFAULT", request.ProductType);

            // Quick risk assessment
            var risk = new RiskAssessment
            {
                RiskScore = 50, // Neutral for estimates
                RiskTier = "Standard",
                FactorScores = new List<RiskFactorScore>()
            };

            // Create a quote request for premium calculation
            var quoteRequest = new QuoteRequest
            {
                ProductType = request.ProductType,
                AnnualPayroll = request.AnnualPayroll,
                AnnualRevenue = request.AnnualRevenue,
                EmployeeCount = request.EmployeeCount,
                CoverageLimit = request.CoverageLimit,
                Deductible = request.Deductible
            };

            // Calculate premium
            var premium = _dataService.CalculatePremium(quoteRequest, rate, risk);

            var response = new PremiumEstimateResponse
            {
                EstimatedAnnualPremium = premium.AnnualPremium,
                EstimatedMonthlyPremium = premium.MonthlyPremium,
                BasePremium = premium.BasePremium,
                MinimumPremium = premium.MinimumPremium,
                StateTaxRate = rate.StateTaxRate,
                Note = "This is an estimate. Final premium may vary based on full underwriting."
            };

            return new OkObjectResult(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing request body");
            return new BadRequestObjectResult(new { error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating premium estimate");
            return new ObjectResult(new { error = "Internal server error" })
            {
                StatusCode = 500
            };
        }
    }
}

/// <summary>
/// Request model for premium estimation.
/// </summary>
public class PremiumEstimateRequest
{
    public string ProductType { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string? ClassificationCode { get; set; }
    public decimal AnnualPayroll { get; set; }
    public decimal AnnualRevenue { get; set; }
    public int EmployeeCount { get; set; }
    public decimal CoverageLimit { get; set; } = 1000000m;
    public decimal Deductible { get; set; } = 1000m;
}

/// <summary>
/// Response model for premium estimation.
/// </summary>
public class PremiumEstimateResponse
{
    public decimal EstimatedAnnualPremium { get; set; }
    public decimal EstimatedMonthlyPremium { get; set; }
    public decimal BasePremium { get; set; }
    public decimal MinimumPremium { get; set; }
    public decimal StateTaxRate { get; set; }
    public string Note { get; set; } = string.Empty;
}
