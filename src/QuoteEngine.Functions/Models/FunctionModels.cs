namespace QuoteEngine.Functions.Models;

/// <summary>
/// Quote request model for Azure Functions.
/// </summary>
public class QuoteRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string ClassificationCode { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal AnnualPayroll { get; set; }
    public decimal AnnualRevenue { get; set; }
    public int EmployeeCount { get; set; }
    public int YearsInBusiness { get; set; }
    public decimal CoverageLimit { get; set; } = 1000000m;
    public decimal Deductible { get; set; } = 1000m;
    public string? ContactEmail { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

/// <summary>
/// Quote response model for Azure Functions.
/// </summary>
public class QuoteResponse
{
    public string QuoteNumber { get; set; } = string.Empty;
    public DateTime QuoteDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public decimal CoverageLimit { get; set; }
    public decimal Deductible { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime PolicyExpirationDate { get; set; }
    public PremiumBreakdown Premium { get; set; } = new();
    public RiskAssessment RiskAssessment { get; set; } = new();
    public bool IsEligible { get; set; }
    public List<string> EligibilityMessages { get; set; } = new();
    public string ProcessingTimeMs { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "1.0";
}

public class PremiumBreakdown
{
    public decimal BasePremium { get; set; }
    public List<PremiumAdjustment> Adjustments { get; set; } = new();
    public decimal TotalAdjustments { get; set; }
    public decimal Subtotal { get; set; }
    public decimal StateTax { get; set; }
    public decimal PolicyFee { get; set; }
    public decimal AnnualPremium { get; set; }
    public decimal MonthlyPremium { get; set; }
    public decimal MinimumPremium { get; set; }
}

public class PremiumAdjustment
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Factor { get; set; }
    public decimal Amount { get; set; }
}

public class RiskAssessment
{
    public int RiskScore { get; set; }
    public string RiskTier { get; set; } = string.Empty;
    public List<RiskFactorScore> FactorScores { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public class RiskFactorScore
{
    public string FactorName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Weight { get; set; }
    public string Impact { get; set; } = string.Empty;
}

/// <summary>
/// Rate lookup result
/// </summary>
public class RateEntry
{
    public string StateCode { get; set; } = string.Empty;
    public string ClassificationCode { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal BaseRate { get; set; }
    public decimal MinPremium { get; set; }
    public decimal StateTaxRate { get; set; }
}
