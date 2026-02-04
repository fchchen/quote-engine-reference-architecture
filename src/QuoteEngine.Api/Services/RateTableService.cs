using QuoteEngine.Api.Models;

namespace QuoteEngine.Api.Services;

/// <summary>
/// In-memory rate table service for demo purposes.
/// In production, this would query the database.
///
/// INTERVIEW TIP: This is a good example of using an in-memory implementation
/// for testing/demo while the interface allows swapping to a database-backed
/// implementation for production.
/// </summary>
public class InMemoryRateTableService : IRateTableService
{
    private readonly ILogger<InMemoryRateTableService> _logger;
    private readonly List<RateTableEntry> _rateTable;
    private readonly List<ClassificationCode> _classificationCodes;

    public InMemoryRateTableService(ILogger<InMemoryRateTableService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rateTable = InitializeRateTable();
        _classificationCodes = InitializeClassificationCodes();
    }

    /// <inheritdoc />
    public Task<RateTableEntry?> GetRateAsync(
        string stateCode,
        string classificationCode,
        ProductType productType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Looking up rate for State: {State}, Class: {Class}, Product: {Product}",
            stateCode, classificationCode, productType);

        // First, try exact match
        var entry = _rateTable.FirstOrDefault(r =>
            r.StateCode.Equals(stateCode, StringComparison.OrdinalIgnoreCase) &&
            r.ClassificationCode.Equals(classificationCode, StringComparison.OrdinalIgnoreCase) &&
            r.ProductType == productType &&
            r.IsActive);

        // If no exact match, try state + product (default classification)
        if (entry == null)
        {
            entry = _rateTable.FirstOrDefault(r =>
                r.StateCode.Equals(stateCode, StringComparison.OrdinalIgnoreCase) &&
                r.ClassificationCode == "DEFAULT" &&
                r.ProductType == productType &&
                r.IsActive);
        }

        // If still no match, try product-only default
        if (entry == null)
        {
            entry = _rateTable.FirstOrDefault(r =>
                r.StateCode == "DEFAULT" &&
                r.ClassificationCode == "DEFAULT" &&
                r.ProductType == productType &&
                r.IsActive);
        }

        if (entry != null)
        {
            _logger.LogDebug("Found rate entry: BaseRate={BaseRate}", entry.BaseRate);
        }
        else
        {
            _logger.LogWarning("No rate entry found");
        }

        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ClassificationCode>> GetClassificationCodesAsync(
        ProductType productType,
        CancellationToken cancellationToken = default)
    {
        var codes = _classificationCodes
            .Where(c => c.ProductType == productType && c.IsActive)
            .OrderBy(c => c.Code);

        return Task.FromResult<IEnumerable<ClassificationCode>>(codes.ToList());
    }

    #region Data Initialization

    private static List<RateTableEntry> InitializeRateTable()
    {
        var entries = new List<RateTableEntry>();
        var states = new[] { "CA", "TX", "NY", "FL", "IL", "PA", "OH", "GA", "NC", "MI", "DEFAULT" };
        var id = 1;

        foreach (var state in states)
        {
            // Workers' Compensation rates (per $100 of payroll)
            entries.AddRange(new[]
            {
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "8810", ProductType = ProductType.WorkersCompensation,
                    BaseRate = state == "CA" ? 2.50m : 1.85m, MinPremium = 1000m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                },
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "8742", ProductType = ProductType.WorkersCompensation,
                    BaseRate = state == "CA" ? 1.20m : 0.95m, MinPremium = 800m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                },
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "5183", ProductType = ProductType.WorkersCompensation,
                    BaseRate = state == "CA" ? 8.50m : 6.25m, MinPremium = 2500m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                },
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "DEFAULT", ProductType = ProductType.WorkersCompensation,
                    BaseRate = 2.00m, MinPremium = 1000m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                },

                // General Liability rates (per $1000 of revenue)
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "41677", ProductType = ProductType.GeneralLiability,
                    BaseRate = 5.50m, MinPremium = 500m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                },
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "91111", ProductType = ProductType.GeneralLiability,
                    BaseRate = 12.00m, MinPremium = 750m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                },
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "DEFAULT", ProductType = ProductType.GeneralLiability,
                    BaseRate = 7.50m, MinPremium = 500m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                },

                // BOP rates
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "DEFAULT", ProductType = ProductType.BusinessOwnersPolicy,
                    BaseRate = 9.00m, MinPremium = 750m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                },

                // Commercial Auto rates
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "DEFAULT", ProductType = ProductType.CommercialAuto,
                    BaseRate = 1200m, MinPremium = 1500m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                },

                // Professional Liability rates
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "DEFAULT", ProductType = ProductType.ProfessionalLiability,
                    BaseRate = 4.25m, MinPremium = 1000m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                },

                // Cyber Liability rates
                new RateTableEntry
                {
                    Id = id++, StateCode = state, ClassificationCode = "DEFAULT", ProductType = ProductType.CyberLiability,
                    BaseRate = 2.50m, MinPremium = 500m, StateTaxRate = GetStateTaxRate(state),
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true
                }
            });
        }

        return entries;
    }

    private static decimal GetStateTaxRate(string stateCode)
    {
        return stateCode switch
        {
            "CA" => 0.0328m,  // 3.28%
            "TX" => 0.018m,   // 1.8%
            "NY" => 0.035m,   // 3.5%
            "FL" => 0.015m,   // 1.5%
            "IL" => 0.035m,   // 3.5%
            "PA" => 0.02m,    // 2%
            "OH" => 0.015m,   // 1.5%
            "GA" => 0.025m,   // 2.5%
            "NC" => 0.02m,    // 2%
            "MI" => 0.025m,   // 2.5%
            _ => 0.02m        // Default 2%
        };
    }

    private static List<ClassificationCode> InitializeClassificationCodes()
    {
        return new List<ClassificationCode>
        {
            // Workers' Comp Classifications
            new() { Code = "8810", Description = "Clerical Office Employees", ProductType = ProductType.WorkersCompensation, BaseRate = 0.25m, HazardGroup = "A", IsActive = true },
            new() { Code = "8742", Description = "Salespersons - Outside", ProductType = ProductType.WorkersCompensation, BaseRate = 0.35m, HazardGroup = "A", IsActive = true },
            new() { Code = "8820", Description = "Attorneys - All Employees", ProductType = ProductType.WorkersCompensation, BaseRate = 0.30m, HazardGroup = "A", IsActive = true },
            new() { Code = "8832", Description = "Physicians - All Employees", ProductType = ProductType.WorkersCompensation, BaseRate = 0.45m, HazardGroup = "B", IsActive = true },
            new() { Code = "5183", Description = "Plumbing - Residential", ProductType = ProductType.WorkersCompensation, BaseRate = 4.50m, HazardGroup = "D", IsActive = true },
            new() { Code = "5190", Description = "Electrical Work", ProductType = ProductType.WorkersCompensation, BaseRate = 3.80m, HazardGroup = "D", IsActive = true },
            new() { Code = "5403", Description = "Carpentry - Residential", ProductType = ProductType.WorkersCompensation, BaseRate = 5.20m, HazardGroup = "D", IsActive = true },
            new() { Code = "9015", Description = "Building Operation", ProductType = ProductType.WorkersCompensation, BaseRate = 2.10m, HazardGroup = "C", IsActive = true },
            new() { Code = "9586", Description = "Parking Lots - Attended", ProductType = ProductType.WorkersCompensation, BaseRate = 2.80m, HazardGroup = "C", IsActive = true },

            // General Liability Classifications
            new() { Code = "41677", Description = "Restaurant - No Liquor", ProductType = ProductType.GeneralLiability, BaseRate = 5.50m, HazardGroup = "B", IsActive = true },
            new() { Code = "41675", Description = "Restaurant - With Liquor", ProductType = ProductType.GeneralLiability, BaseRate = 8.50m, HazardGroup = "C", IsActive = true },
            new() { Code = "91111", Description = "Retail Store", ProductType = ProductType.GeneralLiability, BaseRate = 3.20m, HazardGroup = "B", IsActive = true },
            new() { Code = "41650", Description = "Office - Professional", ProductType = ProductType.GeneralLiability, BaseRate = 2.50m, HazardGroup = "A", IsActive = true },
            new() { Code = "91302", Description = "Contractor - General", ProductType = ProductType.GeneralLiability, BaseRate = 12.00m, HazardGroup = "D", IsActive = true },
            new() { Code = "91340", Description = "Manufacturer - Light", ProductType = ProductType.GeneralLiability, BaseRate = 6.50m, HazardGroup = "C", IsActive = true },
            new() { Code = "91341", Description = "Manufacturer - Heavy", ProductType = ProductType.GeneralLiability, BaseRate = 9.00m, HazardGroup = "D", IsActive = true }
        };
    }

    #endregion
}
