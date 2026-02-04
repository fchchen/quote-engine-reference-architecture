# Quote Engine Reference Architecture

A production-ready reference architecture for building scalable, real-time commercial insurance quoting platforms. Demonstrates high-throughput API design, async processing patterns, and financial transaction integrity using .NET Core, SQL Server, and Azure Functions.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Angular 17 Frontend                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐ │
│  │ Quote Form  │  │Quote Result │  │  Business   │  │  Quote History  │ │
│  │  (Stepper)  │  │  Display    │  │   Search    │  │    (Table)      │ │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └────────┬────────┘ │
│         │                │                │                  │          │
│  ┌──────┴────────────────┴────────────────┴──────────────────┴────────┐ │
│  │                    Services (Signals + RxJS)                        │ │
│  │  • QuoteService (state management)                                  │ │
│  │  • BusinessLookupService (search with debounce)                     │ │
│  │  • RateTableService (cached reference data)                         │ │
│  └──────────────────────────────┬──────────────────────────────────────┘ │
└─────────────────────────────────┼───────────────────────────────────────┘
                                  │ HTTP
                                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         .NET 8 Web API                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────┐  │
│  │ QuoteController │  │BusinessController│  │  RateTableController   │  │
│  └────────┬────────┘  └────────┬────────┘  └────────────┬────────────┘  │
│           │                    │                        │               │
│  ┌────────┴────────────────────┴────────────────────────┴────────────┐  │
│  │                         Services Layer                             │  │
│  │  ┌─────────────┐  ┌────────────────┐  ┌───────────────────────┐   │  │
│  │  │QuoteService │  │ RiskCalculator │  │ InMemoryRateTableSvc  │   │  │
│  │  │ (business   │  │ (premium calc) │  │ (demo data)           │   │  │
│  │  │  logic)     │  │                │  │                       │   │  │
│  │  └─────────────┘  └────────────────┘  └───────────────────────┘   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│           │                                                             │
│  ┌────────┴──────────────────────────────────────────────────────────┐  │
│  │                      Data Layer (EF Core)                          │  │
│  │  QuoteDbContext  │  Repositories  │  Entities                      │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      SQL Server Express (Local)                          │
│  ┌──────────┐  ┌────────┐  ┌───────────┐  ┌────────┐  ┌───────────┐    │
│  │Businesses│  │ Quotes │  │ RateTables│  │Policies│  │ AuditLog  │    │
│  └──────────┘  └────────┘  └───────────┘  └────────┘  └───────────┘    │
└─────────────────────────────────────────────────────────────────────────┘

         OR (Azure Deployment - Free Tier)

┌─────────────────────────────────────────────────────────────────────────┐
│                      Azure Static Web Apps                               │
│  ┌────────────────────────┐  ┌────────────────────────────────────────┐ │
│  │   Angular Frontend     │  │        Azure Functions                  │ │
│  │   (Static Files)       │  │  ┌────────────┐  ┌─────────────────┐   │ │
│  │                        │  │  │ GetQuote   │  │CalculatePremium │   │ │
│  │                        │  │  │ (HTTP)     │  │ (HTTP)          │   │ │
│  │                        │  │  └────────────┘  └─────────────────┘   │ │
│  └────────────────────────┘  │         │                              │ │
│                              │         ▼                              │ │
│                              │  ┌──────────────────────────────────┐  │ │
│                              │  │    MockDataService (JSON)        │  │ │
│                              │  │    (No SQL cost)                 │  │ │
│                              │  └──────────────────────────────────┘  │ │
│                              └────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

## Key Features

- **Signal-First State Management**: Angular 17+ signals for reactive UI state
- **Real-Time Premium Calculation**: Instant quote generation with premium breakdown
- **RESTful API Design**: Versioned endpoints with proper HTTP semantics
- **Dependency Injection**: Loose coupling for testability and flexibility
- **Clean Architecture**: Separation of concerns across layers
- **Free Tier Azure Deployment**: Serverless functions with zero-cost storage

## Tech Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| Frontend | Angular 17+ | UI with signals, Material Design |
| Backend | .NET 8 | RESTful API with DI |
| Database | SQL Server Express | Local development |
| Serverless | Azure Functions | Free tier deployment |
| ORM | Entity Framework Core 8 | Data access |
| Testing | xUnit + Moq | Unit tests |

## Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- SQL Server Express (optional, for local DB)
- Azure Functions Core Tools (optional, for Functions development)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/QuoteEngine-Reference-Architecture.git
   cd QuoteEngine-Reference-Architecture
   ```

2. **Run the .NET API**
   ```bash
   cd src/QuoteEngine.Api
   dotnet run
   ```
   API will be available at `http://localhost:5000`

3. **Run the Angular frontend**
   ```bash
   cd client
   npm install
   npm start
   ```
   Frontend will be available at `http://localhost:4200`

4. **(Optional) Set up SQL Server**
   ```bash
   # Run schema script
   sqlcmd -S localhost\SQLEXPRESS -i database/schema.sql
   # Run seed data
   sqlcmd -S localhost\SQLEXPRESS -i database/seed-data.sql
   ```

### Azure Functions (Free Tier)

```bash
cd src/QuoteEngine.Functions
func start
```
Functions will be available at `http://localhost:7071`

## Project Structure

```
QuoteEngine-Reference-Architecture/
├── README.md                           # This file
├── docs/
│   ├── architecture-overview.md        # System design
│   ├── dotnet-patterns.md              # .NET best practices
│   ├── sql-optimization.md             # Database tuning
│   └── azure-deployment.md             # Deployment guide
├── src/
│   ├── QuoteEngine.Api/                # .NET Core Web API
│   ├── QuoteEngine.Functions/          # Azure Functions
│   ├── QuoteEngine.Data/               # EF Core data layer
│   └── QuoteEngine.Tests/              # Unit tests
├── client/                             # Angular 17+ frontend
├── database/                           # SQL scripts
└── deploy/                             # Deployment configurations
```

## Interview-Relevant Topics

This project demonstrates proficiency in:

### .NET Core
- Dependency Injection lifetimes (Singleton/Scoped/Transient)
- Async/Await patterns with CancellationToken
- Middleware pipeline configuration
- LINQ queries and expressions

### SQL Server
- Index design (Clustered vs Non-clustered)
- Finding and deleting duplicates
- Query optimization with execution plans
- Transaction handling (ACID compliance)

### Angular 17+
- Signals for state management
- Standalone components
- Functional interceptors and guards
- RxJS integration (debounce, switchMap)

### System Design
- RESTful API versioning
- Service layer patterns
- Repository pattern
- Clean architecture

## Sample API Calls

```bash
# Create a quote
curl -X POST http://localhost:5000/api/v1/quote \
  -H "Content-Type: application/json" \
  -d '{
    "businessName": "Test Business LLC",
    "taxId": "12-3456789",
    "businessType": "Technology",
    "stateCode": "CA",
    "productType": "GeneralLiability",
    "annualRevenue": 1000000,
    "annualPayroll": 500000,
    "employeeCount": 10,
    "yearsInBusiness": 5,
    "coverageLimit": 1000000,
    "deductible": 1000
  }'

# Get a quote
curl http://localhost:5000/api/v1/quote/QT-20240101-12345

# Search businesses
curl "http://localhost:5000/api/v1/business/search?searchTerm=Tech&stateCode=CA"

# Get classification codes
curl http://localhost:5000/api/v1/ratetable/classifications/GeneralLiability
```

## Testing

```bash
# Run .NET tests
cd src/QuoteEngine.Tests
dotnet test

# Run Angular tests
cd client
npm test
```

## Documentation

- [Architecture Overview](docs/architecture-overview.md)
- [.NET Patterns Guide](docs/dotnet-patterns.md)
- [SQL Optimization Guide](docs/sql-optimization.md)
- [Azure Deployment Guide](docs/azure-deployment.md)

## License

MIT License - See LICENSE file for details.

---

**Note**: This is a reference architecture for educational and demonstration purposes. It is designed to showcase best practices and patterns without containing any proprietary business logic or company-specific implementations.
