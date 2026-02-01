# DIAX CRM - API Core Deep Dive

The `api-core` is a .NET 8 Web API providing robust services for lead management and financial operations.

## 🏛️ Layered Structure (src/)

| Project | Role | Key Components |
|---------|------|----------------|
| **Diax.Domain** | Core Logic | `Customer`, `FinancialAccount`, `AppLog`, Enums. |
| **Diax.Application** | Use Cases | `CustomerService`, `FinanceService`, DTOs, Mapping logic. |
| **Diax.Infrastructure** | Persistence | `DiaxDbContext`, EF Configurations, SQL Migrations. |
| **Diax.Api** | Interface | `CustomersController`, `LeadsController`, `HealthController`. |

## 📊 Database Schema Highlights

The `DiaxDbContext` implements several custom conventions:
*   **Snake Case Conversion**: Automatically converts PascalCase property names to snake_case in SQL.
*   **UTC DateTime**: All `DateTime` properties are converted to UTC on save and read.
*   **Audit Fields**: `AuditableEntity` tracking (CreatedAt, CreatedBy, etc.) is automated in `SaveChangesAsync`.

### Main Entities
*   `Customer`: Represents both Leads (Status < 4) and converted Customers (Status 4+).
*   `Financial`: `Incomes`, `Expenses`, `CreditCards`, and `CreditCardInvoices`.
*   `Snippets` & `Prompts`: AI-related data for lead communication.

## 📡 Essential Endpoints

*   **/api/v1/customers**: Full CRUD for entities identified as customers.
*   **/api/v1/leads**: Specialized view of `customers` query, filtering for lead statuses (0-3).
*   **/api/v1/finance**: Management of financial accounts, categories, and transactions.
*   **/api/v1/ai-humanize-text**: Endpoint for processing text through AI models.

## 🧪 Testing and Quality
*   Located in `api-core/tests/`.
*   Uses **xUnit** and FluentAssertions.
*   Includes Unit and Integration tests.
