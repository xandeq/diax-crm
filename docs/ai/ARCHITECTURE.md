# DIAX CRM - System Architecture

The project follows a component-based architecture where the **API Core** acts as the central hub (Single Source of Truth), and various clients (Web, Scrapers, n8n) interact with it.

## 🏗️ Architectural Patterns

### Backend: Clean Architecture
The `api-core` is structured into several layers to promote maintainability and testability:
*   **Domain**: Contains entities, enums, value objects, and interfaces. No external dependencies.
*   **Application**: Implements use cases via services, DTOs, and validators (FluentValidation).
*   **Infrastructure**: Deals with persistence (EF Core) and external services.
*   **Api**: The entry point, containing Controllers, Middlewares, and configurations.

### Frontend: Modern Next.js
The `crm-web` uses Next.js 14 with the **App Router**:
*   **Server Components**: For optimized data fetching and security.
*   **Service Layer**: Encapsulated API communication logic in `src/services/`.
*   **Context/State Management**: Uses React Context/Zustand for global UI state.

## 🔄 Execution Flows & Communication

1.  **Lead Acquisition**:
    *   `Scraper (Python)` runs → Finds leads → Saves to CSV or pushes to `API Core`.
    *   `API Core` processes incoming data → Validates → Stores in `SQL Server`.

2.  **User Interaction**:
    *   `CRM Web` fetches data from `API Core` via REST.
    *   Users trigger actions (e.g., "Convert Lead") which calls `API Core` services.

3.  **Advanced Automations**:
    *   `API Core` can trigger `n8n` webhooks for external communications (Email/WhatsApp).
    *   `n8n` processes workflows → Updates `API Core` back with results.

## 🔐 Security and Persistence
*   **Authentication**: JWT-based auth (configured in `api-core/V1/AuthController`).
*   **Database**: SQL Server 2022 with **snake_case** naming convention for tables/columns, managed via EF Core Migrations.
*   **Development**: Uses `.NET User Secrets` to keep sensitive credentials out of the repository.
