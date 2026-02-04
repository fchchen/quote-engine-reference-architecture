using QuoteEngine.Api.Models;

namespace QuoteEngine.Api.Services;

/// <summary>
/// In-memory business lookup service for demo purposes.
/// Demonstrates search with pagination and filtering.
/// </summary>
public class InMemoryBusinessLookupService : IBusinessLookupService
{
    private readonly ILogger<InMemoryBusinessLookupService> _logger;
    private readonly List<BusinessInfo> _businesses;

    public InMemoryBusinessLookupService(ILogger<InMemoryBusinessLookupService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _businesses = InitializeSampleBusinesses();
    }

    /// <inheritdoc />
    public Task<BusinessSearchResponse> SearchAsync(
        BusinessSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching businesses with term: {SearchTerm}", request.SearchTerm);

        IEnumerable<BusinessInfo> query = _businesses.Where(b => b.IsActive);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLowerInvariant();
            query = query.Where(b =>
                b.BusinessName.ToLowerInvariant().Contains(term) ||
                b.DbaName.ToLowerInvariant().Contains(term) ||
                b.TaxId.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.TaxId))
        {
            query = query.Where(b => b.TaxId.Contains(request.TaxId));
        }

        if (!string.IsNullOrWhiteSpace(request.StateCode))
        {
            query = query.Where(b => b.StateCode.Equals(request.StateCode, StringComparison.OrdinalIgnoreCase));
        }

        if (request.BusinessType.HasValue)
        {
            query = query.Where(b => b.BusinessType == request.BusinessType.Value);
        }

        // Get total count before pagination
        var filteredList = query.ToList();
        var totalCount = filteredList.Count;

        // Apply pagination
        var pagedResults = filteredList
            .OrderBy(b => b.BusinessName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Task.FromResult(new BusinessSearchResponse
        {
            Businesses = pagedResults,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        });
    }

    /// <inheritdoc />
    public Task<BusinessInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var business = _businesses.FirstOrDefault(b => b.Id == id);
        return Task.FromResult(business);
    }

    /// <inheritdoc />
    public Task<BusinessInfo?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default)
    {
        var business = _businesses.FirstOrDefault(b =>
            b.TaxId.Equals(taxId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(business);
    }

    private static List<BusinessInfo> InitializeSampleBusinesses()
    {
        return new List<BusinessInfo>
        {
            new()
            {
                Id = 1, BusinessName = "Sunrise Bakery LLC", DbaName = "Sunrise Bakery", TaxId = "12-3456789",
                BusinessType = BusinessType.Restaurant, StateCode = "CA", ClassificationCode = "41677",
                ClassificationDescription = "Restaurant - No Liquor", Address = "123 Main Street",
                City = "Los Angeles", ZipCode = "90001", Phone = "(555) 123-4567",
                Email = "info@sunrisebakery.com", DateEstablished = new DateTime(2015, 3, 15),
                EmployeeCount = 12, AnnualRevenue = 850000m, AnnualPayroll = 320000m,
                IsActive = true, CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new()
            {
                Id = 2, BusinessName = "Tech Solutions Inc", DbaName = "TechSol", TaxId = "23-4567890",
                BusinessType = BusinessType.Technology, StateCode = "CA", ClassificationCode = "8810",
                ClassificationDescription = "Clerical Office Employees", Address = "456 Innovation Drive",
                City = "San Francisco", ZipCode = "94102", Phone = "(555) 234-5678",
                Email = "contact@techsol.com", DateEstablished = new DateTime(2018, 7, 20),
                EmployeeCount = 45, AnnualRevenue = 5200000m, AnnualPayroll = 3800000m,
                IsActive = true, CreatedDate = DateTime.UtcNow.AddYears(-1)
            },
            new()
            {
                Id = 3, BusinessName = "Johnson Plumbing Services", DbaName = "Johnson Plumbing", TaxId = "34-5678901",
                BusinessType = BusinessType.Construction, StateCode = "TX", ClassificationCode = "5183",
                ClassificationDescription = "Plumbing - Residential", Address = "789 Trade Center Blvd",
                City = "Houston", ZipCode = "77001", Phone = "(555) 345-6789",
                Email = "service@johnsonplumbing.com", DateEstablished = new DateTime(2010, 1, 5),
                EmployeeCount = 28, AnnualRevenue = 2100000m, AnnualPayroll = 1400000m,
                IsActive = true, CreatedDate = DateTime.UtcNow.AddYears(-3)
            },
            new()
            {
                Id = 4, BusinessName = "Green Leaf Landscaping", DbaName = "Green Leaf", TaxId = "45-6789012",
                BusinessType = BusinessType.Construction, StateCode = "FL", ClassificationCode = "0042",
                ClassificationDescription = "Landscape Gardening", Address = "321 Garden Way",
                City = "Orlando", ZipCode = "32801", Phone = "(555) 456-7890",
                Email = "info@greenleaflandscape.com", DateEstablished = new DateTime(2012, 4, 10),
                EmployeeCount = 35, AnnualRevenue = 1800000m, AnnualPayroll = 980000m,
                IsActive = true, CreatedDate = DateTime.UtcNow.AddMonths(-18)
            },
            new()
            {
                Id = 5, BusinessName = "Metro Medical Associates", DbaName = "Metro Medical", TaxId = "56-7890123",
                BusinessType = BusinessType.Healthcare, StateCode = "NY", ClassificationCode = "8832",
                ClassificationDescription = "Physicians - All Employees", Address = "555 Healthcare Plaza",
                City = "New York", ZipCode = "10001", Phone = "(555) 567-8901",
                Email = "admin@metromedical.com", DateEstablished = new DateTime(2008, 9, 1),
                EmployeeCount = 85, AnnualRevenue = 12500000m, AnnualPayroll = 7200000m,
                IsActive = true, CreatedDate = DateTime.UtcNow.AddYears(-4)
            },
            new()
            {
                Id = 6, BusinessName = "Quick Stop Convenience", DbaName = "Quick Stop", TaxId = "67-8901234",
                BusinessType = BusinessType.Retail, StateCode = "IL", ClassificationCode = "91111",
                ClassificationDescription = "Retail Store", Address = "888 Commerce Street",
                City = "Chicago", ZipCode = "60601", Phone = "(555) 678-9012",
                Email = "manager@quickstop.com", DateEstablished = new DateTime(2019, 11, 15),
                EmployeeCount = 8, AnnualRevenue = 650000m, AnnualPayroll = 180000m,
                IsActive = true, CreatedDate = DateTime.UtcNow.AddMonths(-6)
            },
            new()
            {
                Id = 7, BusinessName = "Anderson Law Group", DbaName = "Anderson Law", TaxId = "78-9012345",
                BusinessType = BusinessType.ProfessionalServices, StateCode = "PA", ClassificationCode = "8820",
                ClassificationDescription = "Attorneys - All Employees", Address = "100 Legal Center Drive",
                City = "Philadelphia", ZipCode = "19101", Phone = "(555) 789-0123",
                Email = "office@andersonlaw.com", DateEstablished = new DateTime(2005, 6, 1),
                EmployeeCount = 22, AnnualRevenue = 4800000m, AnnualPayroll = 2900000m,
                IsActive = true, CreatedDate = DateTime.UtcNow.AddYears(-5)
            },
            new()
            {
                Id = 8, BusinessName = "Midwest Manufacturing Corp", DbaName = "Midwest Mfg", TaxId = "89-0123456",
                BusinessType = BusinessType.Manufacturing, StateCode = "OH", ClassificationCode = "91340",
                ClassificationDescription = "Manufacturer - Light", Address = "2000 Industrial Parkway",
                City = "Cleveland", ZipCode = "44101", Phone = "(555) 890-1234",
                Email = "info@midwestmfg.com", DateEstablished = new DateTime(1995, 2, 28),
                EmployeeCount = 150, AnnualRevenue = 18500000m, AnnualPayroll = 8500000m,
                IsActive = true, CreatedDate = DateTime.UtcNow.AddYears(-6)
            },
            new()
            {
                Id = 9, BusinessName = "Citywide Transportation Services", DbaName = "Citywide Trans", TaxId = "90-1234567",
                BusinessType = BusinessType.Transportation, StateCode = "GA", ClassificationCode = "7380",
                ClassificationDescription = "Trucking - Local Hauling", Address = "500 Freight Lane",
                City = "Atlanta", ZipCode = "30301", Phone = "(555) 901-2345",
                Email = "dispatch@citywidetrans.com", DateEstablished = new DateTime(2014, 8, 12),
                EmployeeCount = 65, AnnualRevenue = 6200000m, AnnualPayroll = 3100000m,
                IsActive = true, CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new()
            {
                Id = 10, BusinessName = "Premier Real Estate Group", DbaName = "Premier RE", TaxId = "01-2345678",
                BusinessType = BusinessType.RealEstate, StateCode = "NC", ClassificationCode = "8741",
                ClassificationDescription = "Real Estate Agents", Address = "750 Realty Drive",
                City = "Charlotte", ZipCode = "28201", Phone = "(555) 012-3456",
                Email = "contact@premierrealestate.com", DateEstablished = new DateTime(2016, 5, 20),
                EmployeeCount = 18, AnnualRevenue = 2800000m, AnnualPayroll = 1100000m,
                IsActive = true, CreatedDate = DateTime.UtcNow.AddMonths(-14)
            }
        };
    }
}
