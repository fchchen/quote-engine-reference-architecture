namespace QuoteEngine.Api.Models;

/// <summary>
/// Response model containing the calculated quote details.
/// Demonstrates immutable response pattern and comprehensive quote breakdown.
/// </summary>
public class QuoteResponse
{
    public string QuoteNumber { get; init; } = string.Empty;
    public DateTime QuoteDate { get; init; }
    public DateTime ExpirationDate { get; init; }
    public QuoteStatus Status { get; init; }

    // Business Information (echoed back)
    public string BusinessName { get; init; } = string.Empty;
    public BusinessType BusinessType { get; init; }
    public ProductType ProductType { get; init; }
    public string StateCode { get; init; } = string.Empty;

    // Coverage Details
    public decimal CoverageLimit { get; init; }
    public decimal Deductible { get; init; }
    public DateTime EffectiveDate { get; init; }
    public DateTime PolicyExpirationDate { get; init; }

    // Premium Breakdown
    public PremiumBreakdown Premium { get; init; } = new();

    // Risk Assessment
    public RiskAssessment RiskAssessment { get; init; } = new();

    // Eligibility
    public bool IsEligible { get; init; }
    public List<string> EligibilityMessages { get; init; } = new();

    // Metadata
    public string ProcessingTimeMs { get; init; } = string.Empty;
    public string ApiVersion { get; init; } = "1.0";
}

public class PremiumBreakdown
{
    /// <summary>
    /// Base premium before any adjustments
    /// </summary>
    public decimal BasePremium { get; init; }

    /// <summary>
    /// Premium adjustments from risk factors
    /// </summary>
    public List<PremiumAdjustment> Adjustments { get; init; } = new();

    /// <summary>
    /// Sum of all adjustments (positive = surcharge, negative = discount)
    /// </summary>
    public decimal TotalAdjustments { get; init; }

    /// <summary>
    /// Subtotal before taxes and fees
    /// </summary>
    public decimal Subtotal { get; init; }

    /// <summary>
    /// State taxes applied
    /// </summary>
    public decimal StateTax { get; init; }

    /// <summary>
    /// Policy fees
    /// </summary>
    public decimal PolicyFee { get; init; }

    /// <summary>
    /// Final annual premium amount
    /// </summary>
    public decimal AnnualPremium { get; init; }

    /// <summary>
    /// Monthly payment option (if paying in installments)
    /// </summary>
    public decimal MonthlyPremium { get; init; }

    /// <summary>
    /// Minimum premium floor for this product/state
    /// </summary>
    public decimal MinimumPremium { get; init; }
}

public class PremiumAdjustment
{
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public AdjustmentType Type { get; init; }
    public decimal Factor { get; init; }
    public decimal Amount { get; init; }
}

public enum AdjustmentType
{
    Discount = 1,
    Surcharge = 2,
    Credit = 3,
    Debit = 4
}

public class RiskAssessment
{
    /// <summary>
    /// Overall risk score (1-100, higher = more risk)
    /// </summary>
    public int RiskScore { get; init; }

    /// <summary>
    /// Risk tier classification
    /// </summary>
    public RiskTier RiskTier { get; init; }

    /// <summary>
    /// Individual risk factor scores
    /// </summary>
    public List<RiskFactorScore> FactorScores { get; init; } = new();

    /// <summary>
    /// Underwriting notes
    /// </summary>
    public List<string> Notes { get; init; } = new();
}

public class RiskFactorScore
{
    public string FactorName { get; init; } = string.Empty;
    public int Score { get; init; }
    public int Weight { get; init; }
    public string Impact { get; init; } = string.Empty;
}

public enum RiskTier
{
    Preferred = 1,
    Standard = 2,
    NonStandard = 3,
    Decline = 4
}

public enum QuoteStatus
{
    Draft = 1,
    Quoted = 2,
    Referred = 3,
    Declined = 4,
    Expired = 5,
    Bound = 6
}
