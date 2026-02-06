using System.Diagnostics;
using QuoteEngine.Api.Models;

namespace QuoteEngine.Api.Services;

/// <summary>
/// Quote calculation service implementing business logic.
///
/// INTERVIEW TALKING POINTS:
/// - Uses constructor DI (preferred over property injection)
/// - Async/await pattern with proper CancellationToken usage
/// - Single Responsibility: delegates risk calculation to IRiskCalculator
/// - Demonstrates SOLID principles throughout
/// </summary>
public class QuoteService : IQuoteService
{
    private readonly IRiskCalculator _riskCalculator;
    private readonly IRateTableService _rateTableService;
    private readonly ILogger<QuoteService> _logger;

    // Thread-safe in-memory storage for demo purposes
    // In production, this would use the database via repository
    private static readonly Dictionary<string, QuoteResponse> _quoteStore = new();
    private static readonly Dictionary<string, List<string>> _taxIdIndex = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Constructor demonstrating dependency injection.
    /// INTERVIEW TIP: Dependencies are injected, not created - enables testing and loose coupling.
    /// </summary>
    public QuoteService(
        IRiskCalculator riskCalculator,
        IRateTableService rateTableService,
        ILogger<QuoteService> logger)
    {
        _riskCalculator = riskCalculator ?? throw new ArgumentNullException(nameof(riskCalculator));
        _rateTableService = rateTableService ?? throw new ArgumentNullException(nameof(rateTableService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<QuoteResponse?> CalculateQuoteAsync(QuoteRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Processing quote request for {BusinessName}, Product: {ProductType}",
            request.BusinessName,
            request.ProductType);

        try
        {
            // Step 1: Check eligibility
            var eligibility = await CheckEligibilityAsync(request, cancellationToken);
            if (!eligibility.IsEligible)
            {
                _logger.LogWarning(
                    "Business {BusinessName} not eligible: {Reasons}",
                    request.BusinessName,
                    string.Join(", ", eligibility.Messages));

                return CreateDeclinedQuote(request, eligibility, stopwatch.ElapsedMilliseconds);
            }

            // Step 2: Get rate table entry
            // ConfigureAwait(false) - don't need to resume on original context
            var rateEntry = await _rateTableService
                .GetRateAsync(request.StateCode, request.ClassificationCode, request.ProductType, cancellationToken)
                .ConfigureAwait(false);

            if (rateEntry is null)
            {
                _logger.LogWarning(
                    "No rate found for State: {State}, Class: {Class}, Product: {Product}",
                    request.StateCode,
                    request.ClassificationCode,
                    request.ProductType);

                return CreateDeclinedQuote(request, new EligibilityResult
                {
                    IsEligible = false,
                    Messages = new List<string> { "No rate available for the requested coverage" }
                }, stopwatch.ElapsedMilliseconds);
            }

            // Step 3: Calculate risk assessment
            var riskAssessment = _riskCalculator.CalculateRisk(request);

            // Step 4: Calculate premium
            var premium = _riskCalculator.CalculatePremium(request, riskAssessment, rateEntry);

            // Step 5: Build response
            var quoteNumber = GenerateQuoteNumber();
            var effectiveDate = request.EffectiveDate ?? DateTime.UtcNow.AddDays(1).Date;

            var response = new QuoteResponse
            {
                QuoteNumber = quoteNumber,
                QuoteDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                Status = riskAssessment.RiskTier == RiskTier.Decline ? QuoteStatus.Declined : QuoteStatus.Quoted,
                BusinessName = request.BusinessName,
                BusinessType = request.BusinessType,
                ProductType = request.ProductType,
                StateCode = request.StateCode,
                CoverageLimit = request.CoverageLimit,
                Deductible = request.Deductible,
                EffectiveDate = effectiveDate,
                PolicyExpirationDate = effectiveDate.AddYears(1),
                Premium = premium,
                RiskAssessment = riskAssessment,
                IsEligible = riskAssessment.RiskTier != RiskTier.Decline,
                EligibilityMessages = riskAssessment.RiskTier == RiskTier.Decline
                    ? new List<string> { "Risk assessment indicates decline" }
                    : eligibility.Warnings,
                ProcessingTimeMs = $"{stopwatch.ElapsedMilliseconds}ms"
            };

            // Store quote and index by taxId
            StoreQuote(response, request.TaxId);

            stopwatch.Stop();
            _logger.LogInformation(
                "Quote {QuoteNumber} generated. Premium: {Premium:C}, Processing time: {Time}ms",
                quoteNumber,
                premium.AnnualPremium,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Quote calculation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating quote for {BusinessName}", request.BusinessName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<QuoteResponse?> GetQuoteAsync(string quoteNumber, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _quoteStore.TryGetValue(quoteNumber, out var quote);
            return Task.FromResult(quote);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<QuoteResponse>> GetQuoteHistoryAsync(string taxId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_taxIdIndex.TryGetValue(taxId, out var quoteNumbers))
            {
                return Task.FromResult<IEnumerable<QuoteResponse>>(new List<QuoteResponse>());
            }

            var quotes = quoteNumbers
                .Where(qn => _quoteStore.ContainsKey(qn))
                .Select(qn => _quoteStore[qn])
                .OrderByDescending(q => q.QuoteDate)
                .ToList();

            return Task.FromResult<IEnumerable<QuoteResponse>>(quotes);
        }
    }

    /// <inheritdoc />
    public Task<EligibilityResult> CheckEligibilityAsync(QuoteRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var warnings = new List<string>();
        string? referralReason = null;

        // Rule 1: Business must have been operating for at least 1 year
        if (request.YearsInBusiness < 1)
        {
            messages.Add("Business must have at least 1 year of operating history");
        }

        // Rule 2: Workers Comp requires at least 1 employee
        if (request.ProductType == ProductType.WorkersCompensation && request.EmployeeCount < 1)
        {
            messages.Add("Workers' Compensation requires at least 1 employee");
        }

        // Rule 3: High payroll may require referral
        if (request.AnnualPayroll > 10_000_000)
        {
            warnings.Add("High payroll - quote may require underwriter review");
            referralReason = "Annual payroll exceeds $10M threshold";
        }

        // Rule 4: Construction businesses have additional requirements
        if (request.BusinessType == BusinessType.Construction)
        {
            if (request.YearsInBusiness < 3)
            {
                warnings.Add("Construction businesses with less than 3 years experience may have limited coverage options");
            }
        }

        // Rule 5: Coverage limit validation
        if (request.CoverageLimit > 2_000_000 && request.ProductType == ProductType.GeneralLiability)
        {
            warnings.Add("Coverage limits over $2M may require excess liability policy");
        }

        var isEligible = messages.Count == 0;

        return Task.FromResult(new EligibilityResult
        {
            IsEligible = isEligible,
            Messages = messages,
            Warnings = warnings,
            ReferralReason = referralReason
        });
    }

    private QuoteResponse CreateDeclinedQuote(QuoteRequest request, EligibilityResult eligibility, long processingTimeMs)
    {
        return new QuoteResponse
        {
            QuoteNumber = GenerateQuoteNumber(),
            QuoteDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow,
            Status = QuoteStatus.Declined,
            BusinessName = request.BusinessName,
            BusinessType = request.BusinessType,
            ProductType = request.ProductType,
            StateCode = request.StateCode,
            CoverageLimit = request.CoverageLimit,
            Deductible = request.Deductible,
            EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow.AddDays(1),
            PolicyExpirationDate = DateTime.MinValue,
            Premium = new PremiumBreakdown(),
            RiskAssessment = new RiskAssessment { RiskTier = RiskTier.Decline },
            IsEligible = false,
            EligibilityMessages = eligibility.Messages,
            ProcessingTimeMs = $"{processingTimeMs}ms"
        };
    }

    private void StoreQuote(QuoteResponse quote, string? taxId = null)
    {
        lock (_lock)
        {
            _quoteStore[quote.QuoteNumber] = quote;

            if (!string.IsNullOrEmpty(taxId))
            {
                if (!_taxIdIndex.TryGetValue(taxId, out var quoteNumbers))
                {
                    quoteNumbers = new List<string>();
                    _taxIdIndex[taxId] = quoteNumbers;
                }
                quoteNumbers.Add(quote.QuoteNumber);
            }
        }
    }

    private static string GenerateQuoteNumber()
    {
        // Format: QT-YYYYMMDD-XXXXXXXX (GUID-based to avoid collisions)
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var uniquePart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"QT-{datePart}-{uniquePart}";
    }
}
