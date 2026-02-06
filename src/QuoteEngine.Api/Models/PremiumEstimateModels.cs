using System.ComponentModel.DataAnnotations;

namespace QuoteEngine.Api.Models;

/// <summary>
/// Request model for premium estimation without a full quote.
/// </summary>
public class PremiumEstimateRequest
{
    [Required]
    public ProductType ProductType { get; set; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string StateCode { get; set; } = string.Empty;

    public string? ClassificationCode { get; set; }

    public decimal AnnualPayroll { get; set; }

    public decimal AnnualRevenue { get; set; }

    public int EmployeeCount { get; set; }

    [Range(100000, 5000000)]
    public decimal CoverageLimit { get; set; } = 1000000m;

    [Range(500, 50000)]
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
