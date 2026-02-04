using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteEngine.Data.Entities;

/// <summary>
/// Entity representing a business in the database.
/// </summary>
[Table("Businesses")]
public class Business
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string BusinessName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? DbaName { get; set; }

    [Required]
    [StringLength(12)]
    public string TaxId { get; set; } = string.Empty;

    public int BusinessType { get; set; }

    [Required]
    [StringLength(2)]
    public string StateCode { get; set; } = string.Empty;

    [StringLength(10)]
    public string? ClassificationCode { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(10)]
    public string? ZipCode { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Email { get; set; }

    public DateTime? DateEstablished { get; set; }

    public int? EmployeeCount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AnnualRevenue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AnnualPayroll { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // Navigation properties
    public virtual ICollection<Quote> Quotes { get; set; } = new List<Quote>();
}

/// <summary>
/// Entity representing a quote in the database.
/// </summary>
[Table("Quotes")]
public class Quote
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string QuoteNumber { get; set; } = string.Empty;

    public int BusinessId { get; set; }

    public int ProductType { get; set; }

    [Required]
    [StringLength(2)]
    public string StateCode { get; set; } = string.Empty;

    [StringLength(10)]
    public string? ClassificationCode { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CoverageLimit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Deductible { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePremium { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAdjustments { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal StateTax { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PolicyFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AnnualPremium { get; set; }

    public int RiskScore { get; set; }

    public int RiskTier { get; set; }

    public int Status { get; set; }

    public DateTime QuoteDate { get; set; } = DateTime.UtcNow;

    public DateTime QuoteExpirationDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // Navigation property
    [ForeignKey(nameof(BusinessId))]
    public virtual Business? Business { get; set; }
}

/// <summary>
/// Entity representing a rate table entry.
/// </summary>
[Table("RateTables")]
public class RateTable
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(2)]
    public string StateCode { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string ClassificationCode { get; set; } = string.Empty;

    public int ProductType { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal BaseRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinPremium { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal StateTaxRate { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }
}

/// <summary>
/// Entity representing a classification code.
/// </summary>
[Table("ClassificationCodes")]
public class ClassificationCodeEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    public int ProductType { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal BaseRate { get; set; }

    [StringLength(5)]
    public string? HazardGroup { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }
}

/// <summary>
/// Entity representing a policy bound from a quote.
/// </summary>
[Table("Policies")]
public class Policy
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string PolicyNumber { get; set; } = string.Empty;

    public int QuoteId { get; set; }

    public int BusinessId { get; set; }

    public int ProductType { get; set; }

    [Required]
    [StringLength(2)]
    public string StateCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CoverageLimit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Deductible { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AnnualPremium { get; set; }

    public int Status { get; set; }

    public DateTime BoundDate { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // Navigation properties
    [ForeignKey(nameof(QuoteId))]
    public virtual Quote? Quote { get; set; }

    [ForeignKey(nameof(BusinessId))]
    public virtual Business? Business { get; set; }
}
