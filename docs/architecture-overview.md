# Architecture Overview

## System Design

The Quote Engine follows a layered architecture pattern with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────────┐
│                    Presentation Layer                           │
│                    (Angular Frontend)                           │
├─────────────────────────────────────────────────────────────────┤
│                    API Layer                                    │
│                    (Controllers)                                │
├─────────────────────────────────────────────────────────────────┤
│                    Business Logic Layer                         │
│                    (Services)                                   │
├─────────────────────────────────────────────────────────────────┤
│                    Data Access Layer                            │
│                    (Repositories, EF Core)                      │
├─────────────────────────────────────────────────────────────────┤
│                    Database                                     │
│                    (SQL Server)                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow

### Quote Request Flow

```
1. User fills out quote form (Angular)
         │
         ▼
2. QuoteService sends HTTP POST to /api/v1/quote
         │
         ▼
3. QuoteController receives request, validates model
         │
         ▼
4. QuoteService.CalculateQuoteAsync() is called
         │
         ├──► 4a. CheckEligibilityAsync() - validate business rules
         │
         ├──► 4b. RateTableService.GetRateAsync() - lookup rates
         │
         ├──► 4c. RiskCalculator.CalculateRisk() - assess risk
         │
         └──► 4d. RiskCalculator.CalculatePremium() - compute premium
                        │
                        ▼
5. QuoteResponse returned with premium breakdown
         │
         ▼
6. Angular displays results using QuoteResultComponent
```

## Component Responsibilities

### Frontend (Angular)

| Component | Responsibility |
|-----------|----------------|
| QuoteFormComponent | Multi-step wizard for quote input |
| QuoteResultComponent | Display quote with premium breakdown |
| BusinessSearchComponent | Autocomplete with debounced search |
| QuoteHistoryComponent | Table view of past quotes |
| DynamicFieldComponent | Reusable form field renderer |

### Backend (.NET)

| Component | Responsibility |
|-----------|----------------|
| QuoteController | HTTP endpoints for quote operations |
| QuoteService | Business logic, orchestration |
| RiskCalculator | Risk scoring and premium calculation |
| RateTableService | Rate lookups, caching |
| QuoteDbContext | EF Core database context |

## Key Design Decisions

### 1. Signal-First State Management

**Decision**: Use Angular Signals for UI state, RxJS only for HTTP and time-based operations.

**Rationale**:
- Signals provide synchronous, readable state
- Computed values auto-track dependencies
- Effects handle side effects predictably
- RxJS still used for debounce (signals can't do this)

**Example**:
```typescript
// Signals for state
isLoading = signal(false);
quoteHistory = signal<QuoteResponse[]>([]);

// Computed for derived values
totalPremium = computed(() =>
  this.quoteHistory()
    .filter(q => q.isEligible)
    .reduce((sum, q) => sum + q.premium.annualPremium, 0)
);

// RxJS only for HTTP + retry
this.http.post<QuoteResponse>(`${this.apiUrl}/quote`, request).pipe(
  retry({ count: 3, delay: 1000 })
).subscribe(/* ... */);
```

### 2. In-Memory Services for Demo

**Decision**: Use in-memory implementations for rate tables and business lookup.

**Rationale**:
- Enables zero-cost Azure deployment (no SQL)
- Same interfaces as production implementations
- Easy to swap via DI for local SQL development

### 3. Service Layer Pattern

**Decision**: Separate QuoteService (orchestration) from RiskCalculator (calculation).

**Rationale**:
- Single Responsibility Principle
- RiskCalculator is stateless, can be Singleton
- QuoteService may need Scoped for DbContext
- Each can be tested independently

### 4. Async/Await Throughout

**Decision**: All I/O operations are async with CancellationToken support.

**Rationale**:
- Better scalability under load
- User can cancel long operations
- ConfigureAwait(false) in libraries

## Error Handling Strategy

```
┌──────────────────────────────────────────────────────────────┐
│                    Global Exception Middleware                │
│  Catches unhandled exceptions, returns ProblemDetails        │
├──────────────────────────────────────────────────────────────┤
│                    Service Layer                              │
│  Catches specific exceptions, logs, re-throws if critical    │
├──────────────────────────────────────────────────────────────┤
│                    Controller Layer                           │
│  Model validation errors return 400                          │
│  Not found returns 404                                       │
│  Success returns 200 with data                               │
└──────────────────────────────────────────────────────────────┘
```

## Security Considerations

1. **Input Validation**: Model validation attributes on all request DTOs
2. **SQL Injection**: Parameterized queries via EF Core
3. **CORS**: Configured per environment
4. **No Secrets in Code**: Configuration via environment variables

## Performance Optimizations

1. **Database Indexes**: Composite indexes for common query patterns
2. **Response Caching**: Reference data cached with shareReplay
3. **Async Operations**: Non-blocking I/O for scalability
4. **Lazy Loading Routes**: Code splitting in Angular

## Scalability

The architecture supports horizontal scaling:

- **Stateless API**: No server affinity required
- **Azure Functions**: Auto-scales with demand
- **Database**: Connection pooling, read replicas possible
- **Frontend**: Static files via CDN

## Try It Yourself

### Exercise 1: Add a New Product Type

1. Add enum value to `ProductType` in both backend and frontend
2. Add rate entries to `RateTableService`
3. Add calculation logic to `RiskCalculator.CalculateBasePremium()`
4. Update Angular form with new option

### Exercise 2: Add an Eligibility Rule

1. Open `QuoteService.CheckEligibilityAsync()`
2. Add a new rule (e.g., "Cannot quote in certain states")
3. Add unit test for the new rule
4. Test via API

### Exercise 3: Optimize a Query

1. Run a query from `optimization-scripts.sql`
2. Enable execution plan
3. Identify missing index
4. Create index and compare performance
