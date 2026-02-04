namespace QuoteEngine.Api.Models;

/// <summary>
/// Represents business entity information for lookup/autocomplete.
/// Used by the business search functionality.
/// </summary>
public class BusinessInfo
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string DbaName { get; set; } = string.Empty;
    public BusinessType BusinessType { get; set; }
    public string StateCode { get; set; } = string.Empty;
    public string ClassificationCode { get; set; } = string.Empty;
    public string ClassificationDescription { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? DateEstablished { get; set; }
    public int? EmployeeCount { get; set; }
    public decimal? AnnualRevenue { get; set; }
    public decimal? AnnualPayroll { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

/// <summary>
/// Search criteria for business lookup
/// </summary>
public class BusinessSearchRequest
{
    public string? SearchTerm { get; set; }
    public string? TaxId { get; set; }
    public string? StateCode { get; set; }
    public BusinessType? BusinessType { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Paginated search results
/// </summary>
public class BusinessSearchResponse
{
    public List<BusinessInfo> Businesses { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Classification code lookup model
/// </summary>
public class ClassificationCode
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProductType ProductType { get; set; }
    public decimal BaseRate { get; set; }
    public string HazardGroup { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Rate table entry for premium calculation
/// </summary>
public class RateTableEntry
{
    public int Id { get; set; }
    public string StateCode { get; set; } = string.Empty;
    public string ClassificationCode { get; set; } = string.Empty;
    public ProductType ProductType { get; set; }
    public decimal BaseRate { get; set; }
    public decimal MinPremium { get; set; }
    public decimal StateTaxRate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsActive { get; set; } = true;
}
