using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuoteEngine.Data;

/// <summary>
/// Design-time factory for EF Core tooling (migrations).
/// Used by "dotnet ef migrations add" when no connection string is configured at runtime.
/// </summary>
public class QuoteDbContextFactory : IDesignTimeDbContextFactory<QuoteDbContext>
{
    public QuoteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QuoteDbContext>();
        // Use a placeholder connection string for migration generation only.
        // The actual connection string is provided at runtime via configuration.
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=QuoteEngine_Design;Trusted_Connection=True;");
        return new QuoteDbContext(optionsBuilder.Options);
    }
}
