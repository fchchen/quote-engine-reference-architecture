# Azure Deployment Guide

This guide covers deploying the Quote Engine to Azure using the free tier only.

## Architecture for Azure Deployment

```
┌─────────────────────────────────────────────────────────────┐
│                 Azure Static Web Apps (Free)                 │
│  ┌─────────────────────┐    ┌─────────────────────────────┐ │
│  │  Angular Frontend   │    │      Azure Functions        │ │
│  │  (Static Files)     │    │  (Integrated, Free Tier)    │ │
│  │                     │    │                             │ │
│  │  - Built with       │ →  │  - GetQuote                 │ │
│  │    ng build --prod  │    │  - CalculatePremium         │ │
│  │                     │    │  - GetRateTable             │ │
│  │  - Served via CDN   │    │  - HealthCheck              │ │
│  └─────────────────────┘    └─────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                │
                │ /api/* routes
                ▼
┌─────────────────────────────────────────────────────────────┐
│              MockDataService (JSON-based)                    │
│                                                              │
│  - No SQL Server needed                                      │
│  - Rate tables embedded in code                              │
│  - Business data in JSON                                     │
│  - Zero storage cost                                         │
└─────────────────────────────────────────────────────────────┘
```

## Free Tier Limits

| Service | Free Tier Limit | Notes |
|---------|-----------------|-------|
| Azure Static Web Apps | 100GB bandwidth/month | Includes integrated Functions |
| Azure Functions | 1M executions/month | Via Static Web Apps integration |
| Azure Storage | Not needed | Using in-memory data |

## Prerequisites

1. Azure account (free tier)
2. GitHub repository
3. Azure CLI installed

## Deployment Steps

### Step 1: Prepare the Repository

Ensure your repository structure:
```
/
├── client/                 # Angular app
│   ├── src/
│   └── package.json
├── src/
│   └── QuoteEngine.Functions/  # Azure Functions
└── staticwebapp.config.json    # Configuration
```

### Step 2: Create staticwebapp.config.json

Create this file in the repository root:

```json
{
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous"]
    }
  ],
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/images/*.{png,jpg,gif}", "/css/*", "/api/*"]
  },
  "responseOverrides": {
    "404": {
      "rewrite": "/index.html",
      "statusCode": 200
    }
  },
  "globalHeaders": {
    "content-security-policy": "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline' fonts.googleapis.com; font-src 'self' fonts.gstatic.com"
  }
}
```

### Step 3: Create GitHub Actions Workflow

Create `.github/workflows/azure-static-web-apps.yml`:

```yaml
name: Azure Static Web Apps CI/CD

on:
  push:
    branches:
      - main
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches:
      - main

jobs:
  build_and_deploy_job:
    if: github.event_name == 'push' || (github.event_name == 'pull_request' && github.event.action != 'closed')
    runs-on: ubuntu-latest
    name: Build and Deploy Job
    steps:
      - uses: actions/checkout@v3
        with:
          submodules: true

      - name: Build Angular App
        run: |
          cd client
          npm ci
          npm run build -- --configuration production

      - name: Build And Deploy
        id: builddeploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "/client/dist/quote-engine-client/browser"
          api_location: "/src/QuoteEngine.Functions"
          output_location: ""

  close_pull_request_job:
    if: github.event_name == 'pull_request' && github.event.action == 'closed'
    runs-on: ubuntu-latest
    name: Close Pull Request Job
    steps:
      - name: Close Pull Request
        id: closepullrequest
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          action: "close"
```

### Step 4: Create Azure Static Web App

Using Azure CLI:

```bash
# Login to Azure
az login

# Create resource group (if needed)
az group create --name quote-engine-rg --location eastus2

# Create Static Web App
az staticwebapp create \
  --name quote-engine-app \
  --resource-group quote-engine-rg \
  --source https://github.com/YOUR_USERNAME/QuoteEngine-Reference-Architecture \
  --location "East US 2" \
  --branch main \
  --app-location "/client" \
  --api-location "/src/QuoteEngine.Functions" \
  --output-location "dist/quote-engine-client/browser" \
  --login-with-github
```

### Step 5: Get the Deployment Token

```bash
# Get the deployment token
az staticwebapp secrets list \
  --name quote-engine-app \
  --resource-group quote-engine-rg \
  --query "properties.apiKey" -o tsv
```

Add this token as a GitHub secret named `AZURE_STATIC_WEB_APPS_API_TOKEN`.

### Step 6: Configure Environment

Update `client/src/environments/environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: '/api',
  functionsUrl: '/api'
};
```

The `/api` route is automatically proxied to Azure Functions.

### Step 7: Deploy

Push to main branch:

```bash
git add .
git commit -m "Configure Azure deployment"
git push origin main
```

GitHub Actions will automatically:
1. Build the Angular app
2. Deploy static files
3. Deploy Azure Functions
4. Configure routing

## Verify Deployment

1. Go to Azure Portal → Static Web Apps → quote-engine-app
2. Copy the URL (e.g., `https://xxx.azurestaticapps.net`)
3. Open in browser
4. Test API: `https://xxx.azurestaticapps.net/api/health`

## Troubleshooting

### Functions Not Working

1. Check Functions logs in Azure Portal
2. Verify `local.settings.json` has correct values
3. Check CORS configuration

### Build Failures

1. Check GitHub Actions logs
2. Verify `angular.json` output path matches workflow
3. Ensure all dependencies in package.json

### 404 Errors on Routes

1. Verify `staticwebapp.config.json` navigationFallback
2. Check `<base href="/">` in index.html

## Cost Monitoring

Even with free tier, monitor usage:

```bash
# Check usage
az staticwebapp show \
  --name quote-engine-app \
  --resource-group quote-engine-rg \
  --query "sku"
```

## Local Development with Azure Functions

```bash
# Install Azure Functions Core Tools
npm install -g azure-functions-core-tools@4

# Run Functions locally
cd src/QuoteEngine.Functions
func start

# Functions available at http://localhost:7071/api/
```

## Upgrading to Paid Tier

If you need:
- Custom domains with SSL
- More bandwidth
- SQL Server database

1. Upgrade Static Web App to Standard tier
2. Add Azure SQL Database
3. Update connection strings
4. Deploy database schema

```bash
# Upgrade tier
az staticwebapp update \
  --name quote-engine-app \
  --resource-group quote-engine-rg \
  --sku Standard
```
