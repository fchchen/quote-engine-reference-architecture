using QuoteEngine.Api.Models;

namespace QuoteEngine.Api.Services;

/// <summary>
/// Interface for quote calculation service.
/// Demonstrates interface segregation principle (ISP) and dependency inversion.
///
/// INTERVIEW TIP: This interface enables unit testing with mocks and
/// allows different implementations (e.g., InMemoryQuoteService for demos,
/// SqlQuoteService for production).
/// </summary>
public interface IQuoteService
{
    /// <summary>
    /// Calculate a quote based on the provided request.
    /// </summary>
    /// <param name="request">Quote request with business and coverage details</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Quote response with premium breakdown, or null if ineligible</returns>
    Task<QuoteResponse?> CalculateQuoteAsync(QuoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a previously generated quote by quote number.
    /// </summary>
    /// <param name="quoteNumber">Unique quote identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quote response if found, null otherwise</returns>
    Task<QuoteResponse?> GetQuoteAsync(string quoteNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get quote history for a business.
    /// </summary>
    /// <param name="taxId">Business tax ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of historical quotes</returns>
    Task<IEnumerable<QuoteResponse>> GetQuoteHistoryAsync(string taxId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if a business is eligible for the requested product.
    /// </summary>
    /// <param name="request">Quote request to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Eligibility result with messages</returns>
    Task<EligibilityResult> CheckEligibilityAsync(QuoteRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for risk calculation service.
/// Separated from IQuoteService to follow Single Responsibility Principle.
/// </summary>
public interface IRiskCalculator
{
    /// <summary>
    /// Calculate risk score and tier for a quote request.
    /// </summary>
    RiskAssessment CalculateRisk(QuoteRequest request);

    /// <summary>
    /// Calculate the premium for a quote request.
    /// </summary>
    PremiumBreakdown CalculatePremium(QuoteRequest request, RiskAssessment riskAssessment, RateTableEntry rateEntry);
}

/// <summary>
/// Interface for rate table lookups.
/// </summary>
public interface IRateTableService
{
    /// <summary>
    /// Get the applicable rate entry for the given criteria.
    /// </summary>
    Task<RateTableEntry?> GetRateAsync(string stateCode, string classificationCode, ProductType productType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all classification codes for a product type.
    /// </summary>
    Task<IEnumerable<ClassificationCode>> GetClassificationCodesAsync(ProductType productType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for business lookup service.
/// </summary>
public interface IBusinessLookupService
{
    /// <summary>
    /// Search for businesses by various criteria.
    /// </summary>
    Task<BusinessSearchResponse> SearchAsync(BusinessSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get business by ID.
    /// </summary>
    Task<BusinessInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get business by Tax ID.
    /// </summary>
    Task<BusinessInfo?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Eligibility check result.
/// </summary>
public class EligibilityResult
{
    public bool IsEligible { get; init; }
    public List<string> Messages { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public string? ReferralReason { get; init; }
}
