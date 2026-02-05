using Microsoft.EntityFrameworkCore;
using QuoteEngine.Data.Entities;

namespace QuoteEngine.Data;

/// <summary>
/// Seeds the database with sample data matching the in-memory services.
/// Idempotent — checks if data exists before inserting.
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(QuoteDbContext context)
    {
        // Ensure database and schema exist
        await context.Database.MigrateAsync();

        // Only seed if tables are empty
        if (await context.Businesses.AnyAsync())
            return;

        await SeedBusinesses(context);
        await SeedRateTables(context);
        await SeedClassificationCodes(context);
        await context.SaveChangesAsync();
    }

    private static async Task SeedBusinesses(QuoteDbContext context)
    {
        var businesses = new List<Business>
        {
            new()
            {
                BusinessName = "Sunrise Bakery LLC", DbaName = "Sunrise Bakery", TaxId = "12-3456789",
                BusinessType = 2, StateCode = "CA", ClassificationCode = "41677",
                Address = "123 Main Street", City = "Los Angeles", ZipCode = "90001",
                Phone = "(555) 123-4567", Email = "info@sunrisebakery.com",
                DateEstablished = new DateTime(2015, 3, 15), EmployeeCount = 12,
                AnnualRevenue = 850000m, AnnualPayroll = 320000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new()
            {
                BusinessName = "Tech Solutions Inc", DbaName = "TechSol", TaxId = "23-4567890",
                BusinessType = 6, StateCode = "CA", ClassificationCode = "8810",
                Address = "456 Innovation Drive", City = "San Francisco", ZipCode = "94102",
                Phone = "(555) 234-5678", Email = "contact@techsol.com",
                DateEstablished = new DateTime(2018, 7, 20), EmployeeCount = 45,
                AnnualRevenue = 5200000m, AnnualPayroll = 3800000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            },
            new()
            {
                BusinessName = "Johnson Plumbing Services", DbaName = "Johnson Plumbing", TaxId = "34-5678901",
                BusinessType = 5, StateCode = "TX", ClassificationCode = "5183",
                Address = "789 Trade Center Blvd", City = "Houston", ZipCode = "77001",
                Phone = "(555) 345-6789", Email = "service@johnsonplumbing.com",
                DateEstablished = new DateTime(2010, 1, 5), EmployeeCount = 28,
                AnnualRevenue = 2100000m, AnnualPayroll = 1400000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-3)
            },
            new()
            {
                BusinessName = "Green Leaf Landscaping", DbaName = "Green Leaf", TaxId = "45-6789012",
                BusinessType = 5, StateCode = "FL", ClassificationCode = "0042",
                Address = "321 Garden Way", City = "Orlando", ZipCode = "32801",
                Phone = "(555) 456-7890", Email = "info@greenleaflandscape.com",
                DateEstablished = new DateTime(2012, 4, 10), EmployeeCount = 35,
                AnnualRevenue = 1800000m, AnnualPayroll = 980000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-18)
            },
            new()
            {
                BusinessName = "Metro Medical Associates", DbaName = "Metro Medical", TaxId = "56-7890123",
                BusinessType = 7, StateCode = "NY", ClassificationCode = "8832",
                Address = "555 Healthcare Plaza", City = "New York", ZipCode = "10001",
                Phone = "(555) 567-8901", Email = "admin@metromedical.com",
                DateEstablished = new DateTime(2008, 9, 1), EmployeeCount = 85,
                AnnualRevenue = 12500000m, AnnualPayroll = 7200000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-4)
            },
            new()
            {
                BusinessName = "Quick Stop Convenience", DbaName = "Quick Stop", TaxId = "67-8901234",
                BusinessType = 1, StateCode = "IL", ClassificationCode = "91111",
                Address = "888 Commerce Street", City = "Chicago", ZipCode = "60601",
                Phone = "(555) 678-9012", Email = "manager@quickstop.com",
                DateEstablished = new DateTime(2019, 11, 15), EmployeeCount = 8,
                AnnualRevenue = 650000m, AnnualPayroll = 180000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            },
            new()
            {
                BusinessName = "Anderson Law Group", DbaName = "Anderson Law", TaxId = "78-9012345",
                BusinessType = 10, StateCode = "PA", ClassificationCode = "8820",
                Address = "100 Legal Center Drive", City = "Philadelphia", ZipCode = "19101",
                Phone = "(555) 789-0123", Email = "office@andersonlaw.com",
                DateEstablished = new DateTime(2005, 6, 1), EmployeeCount = 22,
                AnnualRevenue = 4800000m, AnnualPayroll = 2900000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-5)
            },
            new()
            {
                BusinessName = "Midwest Manufacturing Corp", DbaName = "Midwest Mfg", TaxId = "89-0123456",
                BusinessType = 4, StateCode = "OH", ClassificationCode = "91340",
                Address = "2000 Industrial Parkway", City = "Cleveland", ZipCode = "44101",
                Phone = "(555) 890-1234", Email = "info@midwestmfg.com",
                DateEstablished = new DateTime(1995, 2, 28), EmployeeCount = 150,
                AnnualRevenue = 18500000m, AnnualPayroll = 8500000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-6)
            },
            new()
            {
                BusinessName = "Citywide Transportation Services", DbaName = "Citywide Trans", TaxId = "90-1234567",
                BusinessType = 8, StateCode = "GA", ClassificationCode = "7380",
                Address = "500 Freight Lane", City = "Atlanta", ZipCode = "30301",
                Phone = "(555) 901-2345", Email = "dispatch@citywidetrans.com",
                DateEstablished = new DateTime(2014, 8, 12), EmployeeCount = 65,
                AnnualRevenue = 6200000m, AnnualPayroll = 3100000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new()
            {
                BusinessName = "Premier Real Estate Group", DbaName = "Premier RE", TaxId = "01-2345678",
                BusinessType = 9, StateCode = "NC", ClassificationCode = "8741",
                Address = "750 Realty Drive", City = "Charlotte", ZipCode = "28201",
                Phone = "(555) 012-3456", Email = "contact@premierrealestate.com",
                DateEstablished = new DateTime(2016, 5, 20), EmployeeCount = 18,
                AnnualRevenue = 2800000m, AnnualPayroll = 1100000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-14)
            },
            new()
            {
                BusinessName = "Pacific Coast Brewing Co", DbaName = "PC Brewing", TaxId = "11-2233445",
                BusinessType = 2, StateCode = "WA", ClassificationCode = "41680",
                Address = "200 Brewery Lane", City = "Seattle", ZipCode = "98101",
                Phone = "(555) 111-2233", Email = "hello@pcbrewing.com",
                DateEstablished = new DateTime(2017, 6, 1), EmployeeCount = 32,
                AnnualRevenue = 2400000m, AnnualPayroll = 920000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-20)
            },
            new()
            {
                BusinessName = "Summit Accounting Partners", DbaName = "Summit CPA", TaxId = "22-3344556",
                BusinessType = 10, StateCode = "CO", ClassificationCode = "8810",
                Address = "1500 Financial Plaza", City = "Denver", ZipCode = "80202",
                Phone = "(555) 222-3344", Email = "info@summitcpa.com",
                DateEstablished = new DateTime(2011, 1, 15), EmployeeCount = 25,
                AnnualRevenue = 3200000m, AnnualPayroll = 1850000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-3)
            },
            new()
            {
                BusinessName = "Desert Sun Electric", DbaName = "Desert Electric", TaxId = "33-4455667",
                BusinessType = 5, StateCode = "AZ", ClassificationCode = "5190",
                Address = "800 Volt Avenue", City = "Phoenix", ZipCode = "85001",
                Phone = "(555) 333-4455", Email = "service@desertsunelectric.com",
                DateEstablished = new DateTime(2009, 3, 10), EmployeeCount = 42,
                AnnualRevenue = 4100000m, AnnualPayroll = 2200000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-4)
            },
            new()
            {
                BusinessName = "Bayview Pet Hospital", DbaName = "Bayview Vet", TaxId = "44-5566778",
                BusinessType = 7, StateCode = "CA", ClassificationCode = "8834",
                Address = "350 Animal Care Drive", City = "San Diego", ZipCode = "92101",
                Phone = "(555) 444-5566", Email = "appointments@bayviewvet.com",
                DateEstablished = new DateTime(2013, 9, 22), EmployeeCount = 18,
                AnnualRevenue = 1950000m, AnnualPayroll = 980000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-30)
            },
            new()
            {
                BusinessName = "Lone Star Auto Repair", DbaName = "Lone Star Auto", TaxId = "55-6677889",
                BusinessType = 1, StateCode = "TX", ClassificationCode = "8391",
                Address = "1200 Mechanic Street", City = "Austin", ZipCode = "78701",
                Phone = "(555) 555-6677", Email = "repairs@lonestarauto.com",
                DateEstablished = new DateTime(2006, 11, 8), EmployeeCount = 14,
                AnnualRevenue = 1100000m, AnnualPayroll = 520000m, IsActive = true,
                CreatedDate = DateTime.UtcNow.AddYears(-5)
            }
        };

        await context.Businesses.AddRangeAsync(businesses);
    }

    private static async Task SeedRateTables(QuoteDbContext context)
    {
        var entries = new List<RateTable>();
        var states = new[] { "CA", "TX", "NY", "FL", "IL", "PA", "OH", "GA", "NC", "MI", "DEFAULT" };

        foreach (var state in states)
        {
            var taxRate = GetStateTaxRate(state);

            entries.AddRange(new[]
            {
                // Workers' Compensation rates
                new RateTable
                {
                    StateCode = state, ClassificationCode = "8810", ProductType = 1,
                    BaseRate = state == "CA" ? 2.50m : 1.85m, MinPremium = 1000m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                },
                new RateTable
                {
                    StateCode = state, ClassificationCode = "8742", ProductType = 1,
                    BaseRate = state == "CA" ? 1.20m : 0.95m, MinPremium = 800m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                },
                new RateTable
                {
                    StateCode = state, ClassificationCode = "5183", ProductType = 1,
                    BaseRate = state == "CA" ? 8.50m : 6.25m, MinPremium = 2500m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                },
                new RateTable
                {
                    StateCode = state, ClassificationCode = "DEFAULT", ProductType = 1,
                    BaseRate = 2.00m, MinPremium = 1000m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                },
                // General Liability rates
                new RateTable
                {
                    StateCode = state, ClassificationCode = "41677", ProductType = 2,
                    BaseRate = 5.50m, MinPremium = 500m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                },
                new RateTable
                {
                    StateCode = state, ClassificationCode = "91111", ProductType = 2,
                    BaseRate = 12.00m, MinPremium = 750m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                },
                new RateTable
                {
                    StateCode = state, ClassificationCode = "DEFAULT", ProductType = 2,
                    BaseRate = 7.50m, MinPremium = 500m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                },
                // BOP rates
                new RateTable
                {
                    StateCode = state, ClassificationCode = "DEFAULT", ProductType = 3,
                    BaseRate = 9.00m, MinPremium = 750m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                },
                // Commercial Auto rates
                new RateTable
                {
                    StateCode = state, ClassificationCode = "DEFAULT", ProductType = 4,
                    BaseRate = 1200m, MinPremium = 1500m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                },
                // Professional Liability rates
                new RateTable
                {
                    StateCode = state, ClassificationCode = "DEFAULT", ProductType = 5,
                    BaseRate = 4.25m, MinPremium = 1000m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                },
                // Cyber Liability rates
                new RateTable
                {
                    StateCode = state, ClassificationCode = "DEFAULT", ProductType = 6,
                    BaseRate = 2.50m, MinPremium = 500m, StateTaxRate = taxRate,
                    EffectiveDate = new DateTime(2024, 1, 1), IsActive = true, CreatedDate = DateTime.UtcNow
                }
            });
        }

        await context.RateTables.AddRangeAsync(entries);
    }

    private static async Task SeedClassificationCodes(QuoteDbContext context)
    {
        var codes = new List<ClassificationCodeEntity>
        {
            // Workers' Comp Classifications
            new() { Code = "8810", Description = "Clerical Office Employees", ProductType = 1, BaseRate = 0.25m, HazardGroup = "A", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "8742", Description = "Salespersons - Outside", ProductType = 1, BaseRate = 0.35m, HazardGroup = "A", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "8820", Description = "Attorneys - All Employees", ProductType = 1, BaseRate = 0.30m, HazardGroup = "A", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "8832", Description = "Physicians - All Employees", ProductType = 1, BaseRate = 0.45m, HazardGroup = "B", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "5183", Description = "Plumbing - Residential", ProductType = 1, BaseRate = 4.50m, HazardGroup = "D", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "5190", Description = "Electrical Work", ProductType = 1, BaseRate = 3.80m, HazardGroup = "D", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "5403", Description = "Carpentry - Residential", ProductType = 1, BaseRate = 5.20m, HazardGroup = "D", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "9015", Description = "Building Operation", ProductType = 1, BaseRate = 2.10m, HazardGroup = "C", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "9586", Description = "Parking Lots - Attended", ProductType = 1, BaseRate = 2.80m, HazardGroup = "C", IsActive = true, CreatedDate = DateTime.UtcNow },
            // General Liability Classifications
            new() { Code = "41677", Description = "Restaurant - No Liquor", ProductType = 2, BaseRate = 5.50m, HazardGroup = "B", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "41675", Description = "Restaurant - With Liquor", ProductType = 2, BaseRate = 8.50m, HazardGroup = "C", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "91111", Description = "Retail Store", ProductType = 2, BaseRate = 3.20m, HazardGroup = "B", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "41650", Description = "Office - Professional", ProductType = 2, BaseRate = 2.50m, HazardGroup = "A", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "91302", Description = "Contractor - General", ProductType = 2, BaseRate = 12.00m, HazardGroup = "D", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "91340", Description = "Manufacturer - Light", ProductType = 2, BaseRate = 6.50m, HazardGroup = "C", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Code = "91341", Description = "Manufacturer - Heavy", ProductType = 2, BaseRate = 9.00m, HazardGroup = "D", IsActive = true, CreatedDate = DateTime.UtcNow }
        };

        await context.ClassificationCodes.AddRangeAsync(codes);
    }

    private static decimal GetStateTaxRate(string stateCode) => stateCode switch
    {
        "CA" => 0.0328m,
        "TX" => 0.018m,
        "NY" => 0.035m,
        "FL" => 0.015m,
        "IL" => 0.035m,
        "PA" => 0.02m,
        "OH" => 0.015m,
        "GA" => 0.025m,
        "NC" => 0.02m,
        "MI" => 0.025m,
        _ => 0.02m
    };
}
