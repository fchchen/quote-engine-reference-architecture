using System.ComponentModel.DataAnnotations;

namespace QuoteEngine.Api.Models;

/// <summary>
/// Request model for generating a commercial insurance quote.
/// Demonstrates validation attributes and domain-specific modeling.
/// </summary>
public class QuoteRequest
{
    [Required(ErrorMessage = "Business name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Business name must be between 2 and 200 characters")]
    public string BusinessName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tax ID is required")]
    [RegularExpression(@"^\d{2}-\d{7}$", ErrorMessage = "Tax ID must be in format XX-XXXXXXX")]
    public string TaxId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Business type is required")]
    public BusinessType BusinessType { get; set; }

    [Required(ErrorMessage = "State code is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "State code must be exactly 2 characters")]
    public string StateCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Classification code is required")]
    [StringLength(10, ErrorMessage = "Classification code cannot exceed 10 characters")]
    public string ClassificationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product type is required")]
    public ProductType ProductType { get; set; }

    [Range(10000, 100000000, ErrorMessage = "Annual payroll must be between $10,000 and $100,000,000")]
    public decimal AnnualPayroll { get; set; }

    [Range(10000, 50000000, ErrorMessage = "Annual revenue must be between $10,000 and $50,000,000")]
    public decimal AnnualRevenue { get; set; }

    [Range(1, 10000, ErrorMessage = "Employee count must be between 1 and 10,000")]
    public int EmployeeCount { get; set; }

    [Range(1, 50, ErrorMessage = "Years in business must be between 1 and 50")]
    public int YearsInBusiness { get; set; }

    [Range(100000, 5000000, ErrorMessage = "Coverage limit must be between $100,000 and $5,000,000")]
    public decimal CoverageLimit { get; set; } = 1000000m;

    [Range(500, 50000, ErrorMessage = "Deductible must be between $500 and $50,000")]
    public decimal Deductible { get; set; } = 1000m;

    /// <summary>
    /// Optional risk factors for premium adjustment
    /// </summary>
    public List<RiskFactor>? RiskFactors { get; set; }

    /// <summary>
    /// Contact email for quote delivery
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Requested effective date for the policy
    /// </summary>
    public DateTime? EffectiveDate { get; set; }
}

public enum BusinessType
{
    Retail = 1,
    Restaurant = 2,
    Office = 3,
    Manufacturing = 4,
    Construction = 5,
    Technology = 6,
    Healthcare = 7,
    Transportation = 8,
    RealEstate = 9,
    ProfessionalServices = 10
}

public enum ProductType
{
    WorkersCompensation = 1,
    GeneralLiability = 2,
    BusinessOwnersPolicy = 3,
    CommercialAuto = 4,
    ProfessionalLiability = 5,
    CyberLiability = 6
}

public class RiskFactor
{
    public string FactorCode { get; set; } = string.Empty;
    public string FactorName { get; set; } = string.Empty;
    public decimal FactorValue { get; set; }
    public RiskFactorType FactorType { get; set; }
}

public enum RiskFactorType
{
    Credit = 1,
    Claims = 2,
    Safety = 3,
    Experience = 4,
    Industry = 5
}
