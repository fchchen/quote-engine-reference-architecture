using Microsoft.EntityFrameworkCore;
using QuoteEngine.Api.Models;
using QuoteEngine.Data;

namespace QuoteEngine.Api.Services;

/// <summary>
/// Database-backed business lookup service using EF Core.
/// Replaces InMemoryBusinessLookupService when a connection string is configured.
/// </summary>
public class DatabaseBusinessLookupService : IBusinessLookupService
{
    private readonly QuoteDbContext _dbContext;
    private readonly ILogger<DatabaseBusinessLookupService> _logger;

    public DatabaseBusinessLookupService(QuoteDbContext dbContext, ILogger<DatabaseBusinessLookupService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<BusinessSearchResponse> SearchAsync(
        BusinessSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching businesses in database with term: {SearchTerm}", request.SearchTerm);

        var query = _dbContext.Businesses.Where(b => b.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(b =>
                b.BusinessName.ToLower().Contains(term) ||
                (b.DbaName != null && b.DbaName.ToLower().Contains(term)) ||
                b.TaxId.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.TaxId))
        {
            query = query.Where(b => b.TaxId.Contains(request.TaxId));
        }

        if (!string.IsNullOrWhiteSpace(request.StateCode))
        {
            query = query.Where(b => b.StateCode == request.StateCode);
        }

        if (request.BusinessType.HasValue)
        {
            var businessTypeInt = (int)request.BusinessType.Value;
            query = query.Where(b => b.BusinessType == businessTypeInt);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var businesses = await query
            .OrderBy(b => b.BusinessName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => MapToBusinessInfo(b))
            .ToListAsync(cancellationToken);

        return new BusinessSearchResponse
        {
            Businesses = businesses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<BusinessInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Businesses.FindAsync(new object[] { id }, cancellationToken);
        return entity == null ? null : MapToBusinessInfo(entity);
    }

    /// <inheritdoc />
    public async Task<BusinessInfo?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Businesses
            .FirstOrDefaultAsync(b => b.TaxId == taxId, cancellationToken);
        return entity == null ? null : MapToBusinessInfo(entity);
    }

    private static BusinessInfo MapToBusinessInfo(Data.Entities.Business b) => new()
    {
        Id = b.Id,
        BusinessName = b.BusinessName,
        DbaName = b.DbaName ?? string.Empty,
        TaxId = b.TaxId,
        BusinessType = (BusinessType)b.BusinessType,
        StateCode = b.StateCode,
        ClassificationCode = b.ClassificationCode ?? string.Empty,
        ClassificationDescription = string.Empty,
        Address = b.Address ?? string.Empty,
        City = b.City ?? string.Empty,
        ZipCode = b.ZipCode ?? string.Empty,
        Phone = b.Phone ?? string.Empty,
        Email = b.Email ?? string.Empty,
        DateEstablished = b.DateEstablished,
        EmployeeCount = b.EmployeeCount,
        AnnualRevenue = b.AnnualRevenue,
        AnnualPayroll = b.AnnualPayroll,
        IsActive = b.IsActive,
        CreatedDate = b.CreatedDate,
        ModifiedDate = b.ModifiedDate
    };
}
