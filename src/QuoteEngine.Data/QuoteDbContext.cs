using Microsoft.EntityFrameworkCore;
using QuoteEngine.Data.Entities;

namespace QuoteEngine.Data;

/// <summary>
/// Entity Framework Core DbContext for the Quote Engine database.
///
/// INTERVIEW TALKING POINTS:
/// - DbContext is Scoped by default in DI (one instance per request)
/// - OnModelCreating for Fluent API configuration
/// - Demonstrates index creation, relationships, and data seeding
/// </summary>
public class QuoteDbContext : DbContext
{
    public QuoteDbContext(DbContextOptions<QuoteDbContext> options) : base(options)
    {
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<RateTable> RateTables => Set<RateTable>();
    public DbSet<ClassificationCodeEntity> ClassificationCodes => Set<ClassificationCodeEntity>();
    public DbSet<Policy> Policies => Set<Policy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Isolate all tables under the "quote" schema to coexist with other apps
        modelBuilder.HasDefaultSchema("quote");

        // ====================================================================
        // BUSINESS ENTITY CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<Business>(entity =>
        {
            // Index for TaxId lookups (unique constraint)
            entity.HasIndex(e => e.TaxId)
                .IsUnique()
                .HasDatabaseName("IX_Businesses_TaxId");

            // Index for search by name
            entity.HasIndex(e => e.BusinessName)
                .HasDatabaseName("IX_Businesses_BusinessName");

            // Composite index for state + business type filtering
            entity.HasIndex(e => new { e.StateCode, e.BusinessType })
                .HasDatabaseName("IX_Businesses_StateCode_BusinessType");

            // Index for active businesses
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Businesses_IsActive")
                .HasFilter("[IsActive] = 1");
        });

        // ====================================================================
        // QUOTE ENTITY CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<Quote>(entity =>
        {
            // Unique index on QuoteNumber
            entity.HasIndex(e => e.QuoteNumber)
                .IsUnique()
                .HasDatabaseName("IX_Quotes_QuoteNumber");

            // Index for business quotes lookup
            entity.HasIndex(e => e.BusinessId)
                .HasDatabaseName("IX_Quotes_BusinessId");

            // Composite index for quote search
            entity.HasIndex(e => new { e.BusinessId, e.ProductType, e.Status })
                .HasDatabaseName("IX_Quotes_BusinessId_ProductType_Status");

            // Index for date range queries
            entity.HasIndex(e => e.QuoteDate)
                .HasDatabaseName("IX_Quotes_QuoteDate");

            // Relationship configuration
            entity.HasOne(e => e.Business)
                .WithMany(b => b.Quotes)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ====================================================================
        // RATE TABLE CONFIGURATION
        // ====================================================================
        // INTERVIEW TIP: This index demonstrates the common pattern of
        // indexing columns used in WHERE clauses and JOINs
        modelBuilder.Entity<RateTable>(entity =>
        {
            // Composite index for rate lookups (most frequently used query)
            entity.HasIndex(e => new { e.StateCode, e.ClassificationCode, e.ProductType })
                .HasDatabaseName("IX_RateTables_StateCode_ClassCode_ProductType")
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            // Index for effective date range queries
            entity.HasIndex(e => new { e.EffectiveDate, e.ExpirationDate })
                .HasDatabaseName("IX_RateTables_EffectiveDate_ExpirationDate");
        });

        // ====================================================================
        // CLASSIFICATION CODE CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<ClassificationCodeEntity>(entity =>
        {
            // Index for code lookups
            entity.HasIndex(e => new { e.Code, e.ProductType })
                .IsUnique()
                .HasDatabaseName("IX_ClassificationCodes_Code_ProductType");

            // Index for product type filtering
            entity.HasIndex(e => e.ProductType)
                .HasDatabaseName("IX_ClassificationCodes_ProductType");
        });

        // ====================================================================
        // POLICY CONFIGURATION
        // ====================================================================
        modelBuilder.Entity<Policy>(entity =>
        {
            // Unique index on PolicyNumber
            entity.HasIndex(e => e.PolicyNumber)
                .IsUnique()
                .HasDatabaseName("IX_Policies_PolicyNumber");

            // Index for business policies
            entity.HasIndex(e => e.BusinessId)
                .HasDatabaseName("IX_Policies_BusinessId");

            // Composite index for active policies
            entity.HasIndex(e => new { e.BusinessId, e.Status, e.ExpirationDate })
                .HasDatabaseName("IX_Policies_BusinessId_Status_ExpirationDate");

            // Relationships
            entity.HasOne(e => e.Quote)
                .WithMany()
                .HasForeignKey(e => e.QuoteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
