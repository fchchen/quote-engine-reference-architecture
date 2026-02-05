using Microsoft.EntityFrameworkCore;
using QuoteEngine.Api.Middleware;
using QuoteEngine.Api.Services;
using QuoteEngine.Data;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURE SERVICES (Dependency Injection Container)
// ============================================================================
// INTERVIEW TALKING POINTS:
// - Singleton: Single instance for app lifetime (stateless services)
// - Scoped: One instance per HTTP request (DbContext, Unit of Work)
// - Transient: New instance every time requested (lightweight, stateless)
// ============================================================================

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use camelCase for JSON properties (JavaScript convention)
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Include enum names instead of numbers
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Quote Engine API",
        Version = "v1",
        Description = "Commercial Insurance Quoting Platform API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });
});

// ============================================================================
// REGISTER APPLICATION SERVICES
// ============================================================================

// Singleton - Stateless calculation services (thread-safe, no instance state)
builder.Services.AddSingleton<IRiskCalculator, RiskCalculator>();

// Feature toggle: use database services when a connection string is configured,
// otherwise fall back to in-memory services (CI, local dev without DB)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    // Database mode — register EF Core and database-backed services
    builder.Services.AddDbContext<QuoteDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddScoped<IRateTableService, DatabaseRateTableService>();
    builder.Services.AddScoped<IBusinessLookupService, DatabaseBusinessLookupService>();
}
else
{
    // In-memory mode — no database required
    builder.Services.AddSingleton<IRateTableService, InMemoryRateTableService>();
    builder.Services.AddSingleton<IBusinessLookupService, InMemoryBusinessLookupService>();
}

// Scoped - Services that might use DbContext in production
// Using Scoped ensures new instance per request (important for EF Core DbContext)
builder.Services.AddScoped<IQuoteService, QuoteService>();

// ============================================================================
// CONFIGURE CORS
// ============================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",      // Angular dev server
                "http://localhost:5000",      // Local API
                "https://localhost:5001")     // Local API with HTTPS
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    options.AddPolicy("AllowAzure", policy =>
    {
        // In production, replace with your actual Azure Static Web App URL
        policy.WithOrigins(
                "https://*.azurestaticapps.net",
                "https://*.azurewebsites.net")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ============================================================================
// CONFIGURE HEALTH CHECKS
// ============================================================================
builder.Services.AddHealthChecks();

// ============================================================================
// BUILD THE APPLICATION
// ============================================================================
var app = builder.Build();

// Seed database when running in database mode
if (!string.IsNullOrEmpty(connectionString))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();
    await SeedData.InitializeAsync(dbContext);
}

// ============================================================================
// CONFIGURE HTTP PIPELINE (Middleware)
// ============================================================================
// INTERVIEW TIP: Middleware order matters!
// - Exception handling should be first (catch all)
// - CORS before routing
// - Authentication before Authorization
// - Routing before endpoints
// ============================================================================

// Global exception handling (should be early in pipeline)
app.UseGlobalExceptionHandling();

// Request logging (after exception handling to log errors)
app.UseRequestLogging();

// Development-only middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Quote Engine API v1");
        options.RoutePrefix = "swagger";
    });
}

// CORS (before routing)
app.UseCors(app.Environment.IsDevelopment() ? "AllowAngularDev" : "AllowAzure");

// Request validation
app.UseRequestValidation();

// Routing
app.UseRouting();

// Health check endpoint
app.MapHealthChecks("/health");

// Map controllers
app.MapControllers();

// ============================================================================
// RUN THE APPLICATION
// ============================================================================
app.Run();

// Make Program class accessible for integration testing
public partial class Program { }
