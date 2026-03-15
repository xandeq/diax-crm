# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DIAX CRM is a monorepo with a Next.js 14 frontend (`crm-web/`), a .NET 8 Clean Architecture backend (`api-core/`), and automation workflows. It is a private CRM for Alexandre Queiroz Marketing Digital — single user, not a SaaS product.

**Default ports:**
- Frontend: http://localhost:3000
- API: http://localhost:5062
- Swagger: http://localhost:5062/swagger (disabled in Production)

**Other root folders:**
- `n8n-workflows/` — n8n automation JSON exports (email marketing, WhatsApp routing)
- `scraper-google-email/` — Python Google Maps scraper

---

## Commands

### Frontend (`crm-web/`)

```bash
cd crm-web
npm run dev        # Start dev server
npm run build      # Production build (static export)
```

Environment: copy `crm-web/.env.example` to `crm-web/.env.local` and set `NEXT_PUBLIC_API_BASE_URL=http://localhost:5062`.

### Backend (`api-core/`)

```bash
cd api-core/src/Diax.Api
dotnet run         # Start API locally

cd api-core
dotnet build       # Build all projects
dotnet test        # Run all tests (xUnit)

# Run a single test by name filter
dotnet test --filter "FullyQualifiedName~LeadSanitizationServiceTests.SanitizeAndClassify_WithMojibakeWords"

# Run all tests in a specific class
dotnet test --filter "FullyQualifiedName~LeadSanitizationServiceTests"
```

**Test libraries:** xUnit, Moq, FluentAssertions. Tests are in `api-core/tests/` — `Diax.UnitTests/` and `Diax.IntegrationTests/`.

### Database Migrations (PowerShell scripts in `api-core/scripts/`)

```powershell
# Create a migration (runs locally to generate code only)
cd api-core
.\scripts\add-migration.ps1 -Name "MyMigrationName"

# Apply migrations to PRODUCTION
cd api-core
.\scripts\update-db.ps1
```

The `update-db.ps1` script sets `ASPNETCORE_ENVIRONMENT=Production` and reads the connection string from User Secrets. Run `.\scripts\set-local-db-secret.ps1` first if not yet configured.

### After pushing

Monitor CI/CD: `gh run watch` or `gh run list` to verify deployment.

---

## Critical Rules

### Database — ALWAYS target Production

> **ALL EF Core and SQL operations must run against the Production DB (SmarterASP), never LocalDB.**

- Server: `sql1002.site4now.net` | DB: `db_aaf0a8_diaxcrm`
- Never run `dotnet ef database update` without `ASPNETCORE_ENVIRONMENT=Production`
- The `update-db.ps1` script handles this correctly — always use it
- The API auto-runs `db.Database.Migrate()` on startup (with 45s timeout)

### Frontend Service Layer

- API calls **must** live in `crm-web/src/services/`. Never `fetch` directly in components.
- Use `apiFetch` from `src/services/api.ts` as the base HTTP utility.
- Use shadcn/ui components from `src/components/ui/` for consistency.

---

## Architecture

### Backend — Clean Architecture

```
api-core/src/
  Diax.Api/           # Controllers (V1/), Middleware/, Configuration/, Program.cs
  Diax.Application/   # Services, DTOs, Interfaces (business logic here)
  Diax.Domain/        # Entities, Enums, Value Objects (no external deps)
  Diax.Infrastructure/# EF Core DbContext, Migrations (Data/), Repositories, AI clients
  Diax.Shared/        # Result pattern (Result<T>, Error, PagedResult), extensions
```

- New endpoints → `Diax.Api/Controllers/V1/`
- Business logic → `Diax.Application/<Module>/`
- Schema changes → create migration via `add-migration.ps1`, apply via `update-db.ps1`

**Controller pattern:** All controllers extend `BaseApiController`, which provides:
- `ResolveUserIdAsync(db)` — extracts email from JWT claims, resolves to User.Id via DB lookup
- `HandleResult<T>(Result<T>)` — maps `Result<T>` to HTTP responses (200 on success; 404/400/409/401/403 on error based on error code string matching: `NotFound`→404, `Validation`→400, `Conflict`→409, `Unauthorized`→401, `Forbidden`→403)

**DB conventions (auto-applied in `DiaxDbContext`):**
- PascalCase → `snake_case` column names
- All `DateTime` converted to UTC on save/read via ValueConverter
- `AuditableEntity` fields (CreatedAt, CreatedBy, etc.) auto-tracked in `SaveChangesAsync`
- String columns default to 256 max length
- Decimal precision: 18,2

**Multi-tenancy:** Entities implementing `IUserOwnedEntity` get an automatic query filter by `CurrentUserService.UserId`. Financial entities (Income, Expense, CreditCard, Transaction, etc.) are all filtered this way. **Exception:** `Customer` entity has NO query filter — it's shared across the system, filtered by UserId explicitly in service queries.

**Audit logging:** `AuditSaveChangesInterceptor` in `Diax.Infrastructure/Data/Interceptors/` auto-captures DB change history into `AuditLogEntry`.

**Startup seeding:** `UserSeeder.SeedInitialAdmin()` and `AiDataSeeder.SeedAiProviders()` run on every startup (idempotent). On migration failure, writes `App_Data/startup-error.txt` for FTP diagnostics.

**AI provider credentials:** Encrypted in DB via `ApiKeyEncryptionService` using ASP.NET Data Protection API. Full RBAC: `AiProvider` → `AiModel` → `AiProviderCredential` → `GroupAiModelAccess`/`GroupAiProviderAccess`.

**Configuration priority** (Program.cs):
1. Environment variables (prefix: `DIAX_`)
2. AWS Secrets Manager (`/diax-crm/` path, optional, 10-min reload)
3. User Secrets (Development only)
4. appsettings.{Environment}.json
5. appsettings.json

**DB connection:** SqlConnectionStringBuilder forces TCP, `Encrypt=true`, `TrustServerCertificate=true`, retry logic (10 retries, 30s max delay), 60s command timeout.

**Middleware pipeline order:** CORS → CORS-header-dedup → CorrelationId → ExceptionLogging → RequestResponseLogging → RateLimiter → Swagger (non-Prod) → Serilog → StaticFiles → HTTPS → Auth → Controllers → HealthCheck (`/health`)

### Frontend — Next.js App Router (Static Export)

```
crm-web/src/
  app/               # Routes (App Router): leads/, finance/, customers/, tools/, etc.
  components/        # Reusable components; ui/ for shadcn primitives
  services/          # API clients per domain (api.ts, customers.ts, finance.ts, etc.)
  types/             # TypeScript interfaces matching backend DTOs
  contexts/          # AuthContext (only context)
  lib/               # Utilities (cn(), formatCurrency(), date-utils, CSV export)
```

**Static export:** `next.config.js` sets `output: 'export'` with `trailingSlash: true` and `images: { unoptimized: true }`. Deployed as static HTML to Hostgator/Hostinger.

Navigation is header-only (`components/Header.tsx`) — no sidebar. Dropdown groups: Financeiro, Clientes, Ferramentas, etc.

**Authentication flow:**
- Token stored in `sessionStorage` (cleared on browser close)
- `apiFetch` auto-attaches `Authorization: Bearer` header
- On 401 response, `apiFetch` dispatches `auth:expired` custom event → `AuthContext` listens and auto-logouts
- JWT claims: email from `ClaimTypes.Email` / `sub`; role from `role` / MS schema claim
- `AuthGuard` wraps the app — redirects unauthenticated users to `/login`
- `RoleGuard` for admin-only routes

**Key libraries:** React Hook Form + Zod (forms), TanStack Table (data grids), Tiptap (rich text), Sonner (toasts), Framer Motion (animations), date-fns, Lucide React (icons).

### Key Domain Concepts

**Customer / Lead unified model** — `Customer` entity covers both leads (Status 0–3) and customers (Status 4). `/api/v1/leads` is a filtered view of the same table.

**Status enum:** 0=Lead, 1=Contacted, 2=Qualified, 3=Negotiating, 4=Customer
**Segment enum:** 0=Cold, 1=Warm, 2=Hot
**Source enum:** 1=Manual, 4=Scraping, 10=Import, 11=GoogleMaps

**Email system** — Three distinct flows:
1. **Funnel Outreach** (`/outreach` → `OutreachController`): Auto-segments leads Hot/Warm/Cold, enforces 7-day cooldown and 200/day limit.
2. **Campaign Composer** (`EmailCampaignComposerModal`): Persists a named campaign in DB, then queues recipients.
3. **Bulk Send** (`/email-marketing` → `POST /email-campaigns/send-bulk`): No campaign persistence.

**Background email worker** (`EmailQueueProcessorWorker`): 50/hour, 250/day batch size 50, runs every 5 min. Renders template variables (`{{FirstName}}`, `{{Email}}`, `{{Company}}`), calls Brevo SMTP. Marks items as Processing → Sent/Failed.

**WhatsApp:** Integrated via Evolution API (`EvolutionApi` config section) through `WhatsAppController` and `EvolutionApiClient`.

### Modules at a Glance

| Module | Frontend | Backend |
|--------|----------|---------|
| Financeiro | `src/app/finance/` | `Controllers/V1/Financial*`, `CreditCard*`, `Income*`, `Expense*` / `Application/Finance/` |
| Financial Planner | `src/app/finance/planner/` | `FinancialGoalsController`, `RecurringTransactionsController`, `MonthlySimulationsController` |
| CRM/Leads | `src/app/leads/`, `customers/` | `CustomersController`, `LeadsController` / `Application/Customers/` |
| Email Outreach | `src/app/outreach/`, `email-marketing/` | `OutreachController`, `EmailCampaignsController` / `Application/Outreach/`, `EmailMarketing/` |
| WhatsApp | — | `WhatsAppController` / `Application/WhatsApp/` |
| AI Tools | `src/app/tools/`, `utilities/` | `AiHumanizeTextController`, `PromptGeneratorController`, `HtmlExtractionController`, `AiInsightsController` |
| AI Image Gen | `src/app/tools/image-generation/` | `AiImageGenerationController` / clients: FAL, Gemini, OpenAI, OpenRouter |
| Facebook Ads | `src/app/ads/` | `AdsController` / `Application/Ads/` (Graph API v21.0) |
| Blog | `src/app/admin/blog/` | `BlogController` / `Application/Blog/` |
| Agenda | `src/app/agenda/` | `Appointment`, `AppointmentType` entities |
| Checklists | `src/app/household/checklists/` | `ChecklistsController` / `Application/Household/` |
| Snippets | `src/app/snippet/` | `SnippetsController` |
| Admin | `src/app/admin/` | `UsersController`, `GroupsController`, `AiProvidersController` |
| Audit / Logs | `src/app/logs/` | `AuditLogsController`, `LogsController` / `Application/Logs/`, `Audit/` |

---

## Deploy

Automated via GitHub Actions on push to `main`:
- `deploy-api-core-smarterasp.yml` — compiles .NET, deploys via FTP to SmarterASP (path-filtered to `api-core/`)
- `deploy-crm-web-hostgator.yml` — builds Next.js, deploys via FTP to `ftp.alexandrequeiroz.com.br` (path-filtered to `crm-web/`)
- `reset-remote-db-smarterasp.yml` — **triggered by push to `reset-db` branch** — drops all tables and re-applies migrations. **This deletes all data.**

Required GitHub Secrets:
- **API deploy:** `SMARTERASP_FTP_SERVER`, `SMARTERASP_FTP_USERNAME`, `SMARTERASP_FTP_PASSWORD`, `SMARTERASP_FTP_REMOTE_DIR`, `SMARTERASP_DB_CONNECTIONSTRING`
- **Frontend deploy:** `HOSTGATOR_FTP_SERVER`, `HOSTGATOR_FTP_USERNAME`, `HOSTGATOR_FTP_PASSWORD`, `HOSTGATOR_FTP_REMOTE_DIR`, `CRM_WEB_API_BASE_URL`
- **API runtime:** `DIAX_JWT_KEY`, `DIAX_AUTH_ADMIN_EMAIL`, `DIAX_AUTH_ADMIN_PASSWORD`, AI provider keys (OpenAI, Perplexity, DeepSeek, Gemini, OpenRouter)

### Extrator de Dados Integration

The CRM connects to the Extrator de Dados API for lead scraping. Configuration is **fully managed** and auto-loaded from AWS Secrets Manager.

**AWS Secret:** `tools/diax-extrator` ✓ Auto-configured
- **URL:** `http://185.173.110.180:8000` (Hostinger VPS, Flask backend via Gunicorn)
- **Health:** `GET /api/health` → `{"db":"postgresql","status":"ok"}` ✓ Verified
- **Token:** `2ZvTMgRQBkhPOjb0LOQJ_cf_5ehOgEXxFWTju5NnNe49i_m-HmNOyq6dWwyCH12OLKJZtIJ44A7VyVX8Qew8cA` (auto-generated, secure)
- **Zero manual configuration** — fully automated setup

**Auto-load flow:**
- Backend: `LeadsController.GetExtractorConfig()` reads from AWS SM (path: `/diax-crm/`) → `{ url, token }`
- Frontend: `useEffect` on `/leads/import` calls `apiFetch('/leads/extrator-config')`, auto-populates state
- UI: Shows loading → success → ready for "Buscar Leads" button
- **Status:** ✓ Deployed, tested, fully operational
