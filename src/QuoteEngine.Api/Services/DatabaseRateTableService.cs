using Microsoft.EntityFrameworkCore;
using QuoteEngine.Api.Models;
using QuoteEngine.Data;

namespace QuoteEngine.Api.Services;

/// <summary>
/// Database-backed rate table service using EF Core.
/// Replaces InMemoryRateTableService when a connection string is configured.
/// Uses 3-level fallback: exact match → state+product default → global default.
/// </summary>
public class DatabaseRateTableService : IRateTableService
{
    private readonly QuoteDbContext _dbContext;
    private readonly ILogger<DatabaseRateTableService> _logger;

    public DatabaseRateTableService(QuoteDbContext dbContext, ILogger<DatabaseRateTableService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RateTableEntry?> GetRateAsync(
        string stateCode,
        string classificationCode,
        ProductType productType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Looking up rate in database for State: {State}, Class: {Class}, Product: {Product}",
            stateCode, classificationCode, productType);

        var productTypeInt = (int)productType;

        // First, try exact match
        var entry = await _dbContext.RateTables
            .Where(r => r.StateCode == stateCode
                && r.ClassificationCode == classificationCode
                && r.ProductType == productTypeInt
                && r.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        // If no exact match, try state + product (default classification)
        if (entry == null)
        {
            entry = await _dbContext.RateTables
                .Where(r => r.StateCode == stateCode
                    && r.ClassificationCode == "DEFAULT"
                    && r.ProductType == productTypeInt
                    && r.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // If still no match, try product-only default
        if (entry == null)
        {
            entry = await _dbContext.RateTables
                .Where(r => r.StateCode == "DEFAULT"
                    && r.ClassificationCode == "DEFAULT"
                    && r.ProductType == productTypeInt
                    && r.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (entry != null)
        {
            _logger.LogDebug("Found rate entry: BaseRate={BaseRate}", entry.BaseRate);
            return MapToRateTableEntry(entry, productType);
        }

        _logger.LogWarning("No rate entry found");
        return null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ClassificationCode>> GetClassificationCodesAsync(
        ProductType productType,
        CancellationToken cancellationToken = default)
    {
        var productTypeInt = (int)productType;

        var codes = await _dbContext.ClassificationCodes
            .Where(c => c.ProductType == productTypeInt && c.IsActive)
            .OrderBy(c => c.Code)
            .Select(c => new ClassificationCode
            {
                Code = c.Code,
                Description = c.Description,
                ProductType = (ProductType)c.ProductType,
                BaseRate = c.BaseRate,
                HazardGroup = c.HazardGroup ?? string.Empty,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);

        return codes;
    }

    private static RateTableEntry MapToRateTableEntry(Data.Entities.RateTable r, ProductType productType) => new()
    {
        Id = r.Id,
        StateCode = r.StateCode,
        ClassificationCode = r.ClassificationCode,
        ProductType = productType,
        BaseRate = r.BaseRate,
        MinPremium = r.MinPremium,
        StateTaxRate = r.StateTaxRate,
        EffectiveDate = r.EffectiveDate,
        ExpirationDate = r.ExpirationDate,
        IsActive = r.IsActive
    };
}
