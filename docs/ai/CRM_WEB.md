# DIAX CRM - Web Client (Front-end)

The `crm-web` project is a high-performance admin dashboard built with Next.js 14 and TypeScript.

## 📁 Directory Structure (src/)

*   `app/`: Routing and Layouts (Next.js App Router).
    *   `leads/`: Lead management views.
    *   `finance/`: Financial dashboard and records.
*   `components/`: Reusable UI elements (Buttons, Tables, Modals).
*   `services/`: Communication layer for REST API consumption.
*   `types/`: TypeScript interfaces corresponding to backend DTOs.
*   `contexts/`: React Contexts for auth and global settings.

## 🔗 API Integration

The frontend communicates with the backend via the `apiFetch` utility in `src/services/api.ts`.

### Main Services
*   **customers.ts**: Handles lead and customer CRUD, including pagination and filtering.
*   **finance.ts**: (Extensive) manages accounts, credit card groups, and financial summaries.
*   **auth.ts**: Session management and login/logout flows.

## 🎨 Styling & UI
*   Primary Framework: **Tailwind CSS**.
*   Design approach: Clean, professional admin aesthetic designed for efficient data management.
*   Responsive: Fully compatible with desktop and mobile browsers.

## 🛠️ Configuration
*   `.env.local`: Used for `NEXT_PUBLIC_API_URL` and other environment-specific variables.
*   `next.config.js`: Next.js specific build settings.
