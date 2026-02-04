# Local Development Setup Guide

This guide walks through setting up the Quote Engine for local development.

## Prerequisites

### Required Software

| Software | Version | Download |
|----------|---------|----------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18+ | https://nodejs.org/ |
| Git | Latest | https://git-scm.com/ |

### Optional Software

| Software | Version | Purpose |
|----------|---------|---------|
| SQL Server Express | 2019+ | Full database functionality |
| Azure Functions Core Tools | 4.x | Run Functions locally |
| VS Code | Latest | Recommended IDE |

## Quick Start (Without SQL Server)

The API runs with in-memory data by default, so SQL Server is optional.

### 1. Clone Repository

```bash
git clone https://github.com/your-org/QuoteEngine-Reference-Architecture.git
cd QuoteEngine-Reference-Architecture
```

### 2. Run the API

```bash
cd src/QuoteEngine.Api
dotnet restore
dotnet run
```

API available at: `http://localhost:5000`

### 3. Run the Frontend

```bash
cd client
npm install
npm start
```

Frontend available at: `http://localhost:4200`

### 4. Verify Setup

Open browser to `http://localhost:4200` and try creating a quote.

## Full Setup (With SQL Server)

### 1. Install SQL Server Express

Download and install from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads

During installation:
- Choose "Basic" installation type
- Note the server name (usually `localhost\SQLEXPRESS`)

### 2. Create Database

Using SQLCMD:
```bash
sqlcmd -S localhost\SQLEXPRESS -i database/schema.sql
sqlcmd -S localhost\SQLEXPRESS -i database/seed-data.sql
```

Or using SQL Server Management Studio (SSMS):
1. Connect to `localhost\SQLEXPRESS`
2. Open and execute `database/schema.sql`
3. Open and execute `database/seed-data.sql`

### 3. Configure Connection String

Create `src/QuoteEngine.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "QuoteEngine": "Server=localhost\\SQLEXPRESS;Database=QuoteEngine;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 4. Update Program.cs for SQL

To use SQL Server instead of in-memory services, update `Program.cs`:

```csharp
// Replace in-memory services with SQL implementations
builder.Services.AddDbContext<QuoteDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("QuoteEngine")));

builder.Services.AddScoped<IRateTableService, SqlRateTableService>();
builder.Services.AddScoped<IBusinessLookupService, SqlBusinessLookupService>();
```

## Running Azure Functions Locally

### 1. Install Azure Functions Core Tools

```bash
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

### 2. Start Functions

```bash
cd src/QuoteEngine.Functions
func start
```

Functions available at: `http://localhost:7071`

### 3. Test Functions

```bash
# Health check
curl http://localhost:7071/api/health

# Get quote
curl -X POST http://localhost:7071/api/quote \
  -H "Content-Type: application/json" \
  -d '{"businessName":"Test","businessType":"Technology","stateCode":"CA","productType":"GeneralLiability","annualRevenue":1000000,"yearsInBusiness":5}'
```

## VS Code Setup

### Recommended Extensions

Create `.vscode/extensions.json`:

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.csdevkit",
    "angular.ng-template",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "ms-azuretools.vscode-azurefunctions"
  ]
}
```

### Launch Configuration

Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET API",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/QuoteEngine.Api/bin/Debug/net8.0/QuoteEngine.Api.dll",
      "cwd": "${workspaceFolder}/src/QuoteEngine.Api",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    {
      "name": "Azure Functions",
      "type": "coreclr",
      "request": "attach",
      "processId": "${command:azureFunctions.pickProcess}"
    }
  ],
  "compounds": [
    {
      "name": "Full Stack",
      "configurations": [".NET API"]
    }
  ]
}
```

### Tasks Configuration

Create `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": ["build", "${workspaceFolder}/QuoteEngine.sln"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "test",
      "command": "dotnet",
      "type": "process",
      "args": ["test", "${workspaceFolder}/src/QuoteEngine.Tests"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "ng serve",
      "type": "npm",
      "script": "start",
      "path": "client/",
      "isBackground": true
    }
  ]
}
```

## Troubleshooting

### API Won't Start

1. Check port 5000 is available: `netstat -an | grep 5000`
2. Check .NET SDK: `dotnet --version`
3. Try `dotnet restore` in the API directory

### Angular Build Errors

1. Clear node_modules: `rm -rf node_modules && npm install`
2. Check Node version: `node --version` (should be 18+)
3. Check Angular CLI: `npx ng version`

### SQL Server Connection Issues

1. Verify SQL Server is running: `services.msc` → SQL Server (SQLEXPRESS)
2. Enable TCP/IP in SQL Server Configuration Manager
3. Test connection: `sqlcmd -S localhost\SQLEXPRESS -Q "SELECT 1"`

### CORS Errors

1. Verify API is running on port 5000
2. Check CORS policy in `Program.cs` includes `http://localhost:4200`
3. Clear browser cache
