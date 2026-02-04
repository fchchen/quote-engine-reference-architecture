# Contributing Guidelines

Thank you for your interest in contributing to the Quote Engine Reference Architecture.

## Code Standards

### SOLID Principles

This project adheres to SOLID principles. Here's how they apply:

#### Single Responsibility Principle (SRP)

Each class should have one reason to change.

```csharp
// GOOD: Separate responsibilities
public class QuoteService { /* orchestration */ }
public class RiskCalculator { /* risk calculation */ }
public class RateTableService { /* rate lookups */ }

// BAD: Everything in one class
public class QuoteEngine {
    public void CalculateQuote() { /* does everything */ }
    public void CalculateRisk() { /* ... */ }
    public void GetRates() { /* ... */ }
    public void SendEmail() { /* ... */ }
}
```

#### Open/Closed Principle (OCP)

Open for extension, closed for modification.

```csharp
// GOOD: Add new products without changing existing code
public interface IRiskCalculator {
    RiskAssessment CalculateRisk(QuoteRequest request);
}

// New products extend, don't modify
public class CyberRiskCalculator : IRiskCalculator { }
public class WorkersCompRiskCalculator : IRiskCalculator { }
```

#### Liskov Substitution Principle (LSP)

Derived classes must be substitutable for base classes.

```csharp
// GOOD: InMemoryRateTableService can replace SqlRateTableService
public interface IRateTableService {
    Task<RateTableEntry?> GetRateAsync(...);
}

public class InMemoryRateTableService : IRateTableService { }
public class SqlRateTableService : IRateTableService { }
```

#### Interface Segregation Principle (ISP)

Clients shouldn't depend on interfaces they don't use.

```csharp
// GOOD: Focused interfaces
public interface IQuoteReader {
    Task<QuoteResponse?> GetQuoteAsync(string quoteNumber);
}

public interface IQuoteWriter {
    Task<QuoteResponse?> CalculateQuoteAsync(QuoteRequest request);
}

// BAD: Fat interface
public interface IQuoteService {
    Task<QuoteResponse?> GetQuoteAsync(string quoteNumber);
    Task<QuoteResponse?> CalculateQuoteAsync(QuoteRequest request);
    Task SendEmailAsync(string to, QuoteResponse quote);
    Task GeneratePdfAsync(QuoteResponse quote);
    Task ArchiveQuoteAsync(string quoteNumber);
}
```

#### Dependency Inversion Principle (DIP)

Depend on abstractions, not concretions.

```csharp
// GOOD: Depend on interface
public class QuoteService {
    private readonly IRiskCalculator _riskCalculator;

    public QuoteService(IRiskCalculator riskCalculator) {
        _riskCalculator = riskCalculator;
    }
}

// BAD: Depend on concrete class
public class QuoteService {
    private readonly RiskCalculator _riskCalculator = new RiskCalculator();
}
```

## Coding Conventions

### C# Naming Conventions

```csharp
// PascalCase for: Classes, Methods, Properties, Enums
public class QuoteService { }
public async Task<QuoteResponse> CalculateQuoteAsync() { }
public string BusinessName { get; set; }
public enum QuoteStatus { }

// camelCase for: Parameters, Local variables
public void ProcessQuote(QuoteRequest request)
{
    var quoteNumber = GenerateQuoteNumber();
}

// _camelCase for: Private fields
private readonly ILogger<QuoteService> _logger;
private readonly IRiskCalculator _riskCalculator;

// UPPERCASE for: Constants
public const int MAX_RETRIES = 3;
```

### TypeScript/Angular Conventions

```typescript
// camelCase for: Variables, Functions, Properties
const quoteNumber = 'QT-123';
function calculatePremium() { }

// PascalCase for: Classes, Interfaces, Types, Enums
class QuoteService { }
interface QuoteResponse { }
type ProductType = 'GL' | 'WC';
enum QuoteStatus { }

// Signal naming: No prefix, describe the state
isLoading = signal(false);        // Not: loadingSignal
quoteHistory = signal<Quote[]>([]); // Not: quoteHistorySignal

// Computed naming: Describe the derived value
totalPremium = computed(() => ...);
hasQuotes = computed(() => ...);
```

## Pull Request Process

### Before Submitting

1. **Run Tests**
   ```bash
   # .NET tests
   cd src/QuoteEngine.Tests
   dotnet test

   # Angular tests
   cd client
   npm test
   ```

2. **Check Code Style**
   ```bash
   # Angular lint
   cd client
   npm run lint
   ```

3. **Build Successfully**
   ```bash
   # .NET
   dotnet build

   # Angular
   npm run build
   ```

### PR Description Template

```markdown
## Summary
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Manual testing performed

## Checklist
- [ ] Code follows project conventions
- [ ] Self-review completed
- [ ] Documentation updated if needed
```

## Adding New Features

### Adding a New Product Type

1. **Backend**
   - Add enum value to `ProductType` in `QuoteRequest.cs`
   - Add rate calculation logic in `RiskCalculator.CalculateBasePremium()`
   - Add rate entries in `InMemoryRateTableService`
   - Add unit tests

2. **Frontend**
   - Add enum value to `quote.model.ts`
   - Add product info in `rate-table.service.ts`
   - Update form if product-specific fields needed
   - Add tests

3. **Database**
   - Add rate entries to seed data
   - Add classification codes if applicable

### Adding a New API Endpoint

1. Create/update controller in `Controllers/`
2. Create/update service interface and implementation
3. Register service in `Program.cs` if new
4. Add unit tests
5. Document in README if user-facing

## Commit Messages

Use conventional commits:

```
feat: add cyber liability product type
fix: correct premium calculation for CA
docs: update deployment guide
test: add risk calculator edge cases
refactor: extract validation to separate service
```

## Questions?

Open an issue for:
- Feature requests
- Bug reports
- Questions about architecture
- Help with contributions
