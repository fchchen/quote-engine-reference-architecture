# .NET Patterns and Best Practices

This document covers the .NET patterns demonstrated in this reference architecture, with a focus on topics commonly discussed in technical interviews.

## Dependency Injection

### Lifetime Overview

```csharp
// Singleton: Single instance for app lifetime
// Use for: Stateless services, caches, configuration
builder.Services.AddSingleton<IRiskCalculator, RiskCalculator>();

// Scoped: One instance per HTTP request
// Use for: DbContext, Unit of Work, request-specific state
builder.Services.AddScoped<IQuoteService, QuoteService>();

// Transient: New instance every time requested
// Use for: Lightweight, stateless services
builder.Services.AddTransient<IValidator, RequestValidator>();
```

### Interview Question: Why is DbContext Scoped?

**Answer**: DbContext tracks entity changes for the duration of a request. If it were Singleton:
- Change tracking would grow unbounded
- Concurrent requests would conflict
- No isolation between requests

If it were Transient:
- Multiple instances per request could have inconsistent state
- Transactions across services wouldn't work

### Constructor Injection Pattern

```csharp
public class QuoteService : IQuoteService
{
    private readonly IRiskCalculator _riskCalculator;
    private readonly IRateTableService _rateTableService;
    private readonly ILogger<QuoteService> _logger;

    // Constructor injection - dependencies are required
    public QuoteService(
        IRiskCalculator riskCalculator,
        IRateTableService rateTableService,
        ILogger<QuoteService> logger)
    {
        // Null checks ensure dependencies are provided
        _riskCalculator = riskCalculator ?? throw new ArgumentNullException(nameof(riskCalculator));
        _rateTableService = rateTableService ?? throw new ArgumentNullException(nameof(rateTableService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

**Why Constructor Injection?**
1. Makes dependencies explicit
2. Supports immutability (readonly fields)
3. Easier to test (pass mocks in constructor)
4. Fails fast if dependency missing

## Async/Await Patterns

### Proper CancellationToken Usage

```csharp
public async Task<QuoteResponse?> CalculateQuoteAsync(
    QuoteRequest request,
    CancellationToken cancellationToken = default)
{
    // Pass token to all async operations
    var rateEntry = await _rateTableService
        .GetRateAsync(request.StateCode, request.ClassificationCode,
                      request.ProductType, cancellationToken)
        .ConfigureAwait(false);  // Don't need to resume on original context

    // Check for cancellation at logical points
    cancellationToken.ThrowIfCancellationRequested();

    // Continue processing...
}
```

### Interview Question: What is ConfigureAwait(false)?

**Answer**: By default, after an await, execution resumes on the original SynchronizationContext (e.g., UI thread in desktop apps, HttpContext in ASP.NET).

`ConfigureAwait(false)` says "I don't need the original context, resume on any thread pool thread."

**Use in libraries/services**: Avoids potential deadlocks, slight performance improvement.

**Don't use in**: Controller actions or code that needs HttpContext access.

### Avoiding Deadlocks

```csharp
// BAD: Can deadlock in ASP.NET
public QuoteResponse GetQuote()
{
    return CalculateQuoteAsync(request).Result;  // Blocks!
}

// GOOD: Async all the way
public async Task<QuoteResponse> GetQuote()
{
    return await CalculateQuoteAsync(request);
}
```

## Interface Segregation

### Single Responsibility Interfaces

```csharp
// Good: Focused interfaces
public interface IQuoteService
{
    Task<QuoteResponse?> CalculateQuoteAsync(QuoteRequest request, CancellationToken ct);
    Task<QuoteResponse?> GetQuoteAsync(string quoteNumber, CancellationToken ct);
}

public interface IRiskCalculator
{
    RiskAssessment CalculateRisk(QuoteRequest request);
    PremiumBreakdown CalculatePremium(QuoteRequest request, RiskAssessment risk, RateTableEntry rate);
}

// Bad: God interface
public interface IQuoteEngine
{
    Task<QuoteResponse> Calculate(QuoteRequest request);
    Task<QuoteResponse> Get(string id);
    RiskAssessment CalculateRisk(QuoteRequest request);
    decimal CalculatePremium(...);
    Task<IEnumerable<RateEntry>> GetRates(...);
    // ... dozens more methods
}
```

## LINQ Best Practices

### Common LINQ Operations

```csharp
// Filtering
var eligibleQuotes = quotes.Where(q => q.IsEligible);

// Projection
var quoteNumbers = quotes.Select(q => q.QuoteNumber);

// Aggregation
var totalPremium = quotes.Sum(q => q.Premium.AnnualPremium);
var avgPremium = quotes.Average(q => q.Premium.AnnualPremium);
var maxPremium = quotes.Max(q => q.Premium.AnnualPremium);

// Grouping
var byState = quotes.GroupBy(q => q.StateCode)
    .Select(g => new { State = g.Key, Count = g.Count(), Total = g.Sum(q => q.Premium.AnnualPremium) });

// Ordering
var sorted = quotes.OrderByDescending(q => q.QuoteDate)
    .ThenBy(q => q.BusinessName);

// First/Single with predicate
var latestQuote = quotes.FirstOrDefault(q => q.BusinessId == businessId);
```

### Interview Question: Write LINQ Without IDE

**Question**: "Given a list of quotes, find the total premium for each state, sorted by total descending."

```csharp
var result = quotes
    .Where(q => q.IsEligible)
    .GroupBy(q => q.StateCode)
    .Select(g => new
    {
        State = g.Key,
        QuoteCount = g.Count(),
        TotalPremium = g.Sum(q => q.Premium.AnnualPremium)
    })
    .OrderByDescending(x => x.TotalPremium)
    .ToList();
```

## Middleware Pipeline

### Custom Middleware

```csharp
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Before the next middleware
        _logger.LogInformation("Request started: {Method} {Path}",
            context.Request.Method, context.Request.Path);

        try
        {
            // Call the next middleware
            await _next(context);
        }
        finally
        {
            // After all middleware returns
            stopwatch.Stop();
            _logger.LogInformation("Request completed: {StatusCode} in {ElapsedMs}ms",
                context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
```

### Middleware Order Matters

```csharp
// Order is important!
app.UseGlobalExceptionHandling();  // Catch all exceptions first
app.UseRequestLogging();           // Log after exception handling
app.UseCors("AllowAngularDev");    // CORS before routing
app.UseRouting();                  // Routing before endpoints
app.UseAuthentication();           // Auth before authorization
app.UseAuthorization();            // Authz before endpoints
app.MapControllers();              // Map endpoints last
```

## Validation Patterns

### Model Validation with Data Annotations

```csharp
public class QuoteRequest
{
    [Required(ErrorMessage = "Business name is required")]
    [StringLength(200, MinimumLength = 2)]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{2}-\d{7}$", ErrorMessage = "Format: XX-XXXXXXX")]
    public string TaxId { get; set; } = string.Empty;

    [Range(10000, 100000000, ErrorMessage = "Must be between $10,000 and $100,000,000")]
    public decimal AnnualPayroll { get; set; }

    [EmailAddress]
    public string? ContactEmail { get; set; }
}
```

### [ApiController] Automatic Validation

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class QuoteController : ControllerBase
{
    // [ApiController] automatically:
    // - Returns 400 for invalid model state
    // - Binds request body to parameters
    // - Returns ProblemDetails for errors

    [HttpPost]
    public async Task<ActionResult<QuoteResponse>> CreateQuote(
        [FromBody] QuoteRequest request)  // Auto-validated
    {
        // If we get here, model is valid
        var result = await _quoteService.CalculateQuoteAsync(request);
        return result is null ? BadRequest() : Ok(result);
    }
}
```

## Try It Yourself

### Exercise 1: Add Logging to RiskCalculator

Add ILogger to RiskCalculator and log:
- When calculation starts (with business type)
- Final risk score and tier
- Any risk factors that exceeded thresholds

### Exercise 2: Create Custom Validation Attribute

Create `[ValidStateCode]` attribute that validates the state code is in the list of supported states.

### Exercise 3: Write LINQ Query

Given this data structure:
```csharp
class Quote { string State; string Product; decimal Premium; bool IsEligible; }
```

Write LINQ to find:
1. Average premium by product type for eligible quotes
2. States with more than 10 quotes
3. Top 5 highest premium quotes per state
