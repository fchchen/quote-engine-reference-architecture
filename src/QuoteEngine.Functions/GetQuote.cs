using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using QuoteEngine.Functions.Models;
using QuoteEngine.Functions.Services;

namespace QuoteEngine.Functions;

/// <summary>
/// Azure Function for generating insurance quotes.
/// HTTP Trigger - responds to POST requests.
///
/// INTERVIEW TALKING POINTS:
/// - Serverless architecture - pay per execution
/// - Stateless design - scales horizontally
/// - Uses free tier (1M executions/month)
/// - JSON-based data for zero storage cost
/// </summary>
public class GetQuote
{
    private readonly ILogger<GetQuote> _logger;
    private readonly MockDataService _dataService;

    public GetQuote(ILogger<GetQuote> logger)
    {
        _logger = logger;
        _dataService = new MockDataService();
    }

    /// <summary>
    /// HTTP trigger function to generate a quote.
    /// POST /api/GetQuote
    /// </summary>
    [Function("GetQuote")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "quote")] HttpRequest req)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Quote request received");

        try
        {
            // Parse request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<QuoteRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request is null)
            {
                return new BadRequestObjectResult(new { error = "Invalid request body" });
            }

            // Validate required fields
            var validationErrors = ValidateRequest(request);
            if (validationErrors.Count > 0)
            {
                return new BadRequestObjectResult(new { errors = validationErrors });
            }

            // Get rate for the request
            var rate = _dataService.GetRate(request.StateCode, request.ClassificationCode, request.ProductType);

            // Calculate risk assessment
            var riskAssessment = _dataService.CalculateRisk(request);

            // Check for decline
            if (riskAssessment.RiskTier == "Decline")
            {
                stopwatch.Stop();
                return new OkObjectResult(CreateDeclinedResponse(request, riskAssessment, stopwatch.ElapsedMilliseconds));
            }

            // Calculate premium
            var premium = _dataService.CalculatePremium(request, rate, riskAssessment);

            // Build response
            var response = new QuoteResponse
            {
                QuoteNumber = GenerateQuoteNumber(),
                QuoteDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                Status = "Quoted",
                BusinessName = request.BusinessName,
                BusinessType = request.BusinessType,
                ProductType = request.ProductType,
                StateCode = request.StateCode,
                CoverageLimit = request.CoverageLimit,
                Deductible = request.Deductible,
                EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow.AddDays(1).Date,
                PolicyExpirationDate = (request.EffectiveDate ?? DateTime.UtcNow.AddDays(1).Date).AddYears(1),
                Premium = premium,
                RiskAssessment = riskAssessment,
                IsEligible = true,
                EligibilityMessages = new List<string>(),
                ProcessingTimeMs = $"{stopwatch.ElapsedMilliseconds}ms"
            };

            stopwatch.Stop();
            _logger.LogInformation(
                "Quote {QuoteNumber} generated. Premium: {Premium:C}, Time: {Time}ms",
                response.QuoteNumber,
                premium.AnnualPremium,
                stopwatch.ElapsedMilliseconds);

            return new OkObjectResult(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing request body");
            return new BadRequestObjectResult(new { error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing quote request");
            return new ObjectResult(new { error = "Internal server error" })
            {
                StatusCode = 500
            };
        }
    }

    private static List<string> ValidateRequest(QuoteRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.BusinessName))
            errors.Add("Business name is required");

        if (string.IsNullOrWhiteSpace(request.StateCode))
            errors.Add("State code is required");

        if (string.IsNullOrWhiteSpace(request.ProductType))
            errors.Add("Product type is required");

        if (request.YearsInBusiness < 1)
            errors.Add("Business must have at least 1 year of operating history");

        if (request.AnnualPayroll <= 0 && request.ProductType == "WorkersCompensation")
            errors.Add("Annual payroll is required for Workers' Compensation");

        if (request.AnnualRevenue <= 0)
            errors.Add("Annual revenue is required");

        return errors;
    }

    private static QuoteResponse CreateDeclinedResponse(QuoteRequest request, RiskAssessment risk, long processingTimeMs)
    {
        return new QuoteResponse
        {
            QuoteNumber = GenerateQuoteNumber(),
            QuoteDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow,
            Status = "Declined",
            BusinessName = request.BusinessName,
            BusinessType = request.BusinessType,
            ProductType = request.ProductType,
            StateCode = request.StateCode,
            CoverageLimit = request.CoverageLimit,
            Deductible = request.Deductible,
            RiskAssessment = risk,
            IsEligible = false,
            EligibilityMessages = new List<string> { "Risk assessment indicates decline" },
            ProcessingTimeMs = $"{processingTimeMs}ms"
        };
    }

    private static string GenerateQuoteNumber()
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Random.Shared.Next(10000, 99999);
        return $"QT-{datePart}-{randomPart}";
    }
}
