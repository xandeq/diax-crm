# DIAX CRM - Project Overview

The **DIAX CRM** (Digital Controller) is a central ecosystem for managing marketing and commercial data, designed for the digital marketing landscape. It integrates lead collection (scraping), relationship management (CRM), financial tracking, and AI-driven automation.

## 🚀 Key Features

*   **Lead Acquisition**: Automated scraping of business data (emails, phones) from Google and websites.
*   **Relationship Management**: Centralized storage for leads and customers following a defined sales pipeline.
*   **AI Integration**: Tools for text humanization, prompt generation, and automated lead qualification.
*   **Financial Control**: Tracking of income, expenses, and credit card management.
*   **Workflow Automation**: Deep integration with n8n for complex business logic and communications.

## 🛠️ Technology Stack

| Component | Technology | Description |
|-----------|------------|-------------|
| **Backend** | .NET 8 (C#) | Core API following Clean Architecture & DDD-lite. |
| **Frontend** | Next.js 14+ (TS) | Modern web interface using App Router and React. |
| **Database** | SQL Server 2022 | Relational storage using EF Core for ORM. |
| **Scraper** | Python (Selenium) | Custom tool for data collection. |
| **Automation** | n8n | Low-code workflow orchestration. |
| **Infrastructure** | Docker | Containerized deployment approach (indicated). |

## 📂 Main Directory Structure

*   [`api-core/`](file:///C:/Users/acq20/Desktop/Trabalho/Alexandre%20Queiroz%20Marketing%20Digital/DIAX/CRM/api-core): The heart of the system. Backend API.
*   [`crm-web/`](file:///C:/Users/acq20/Desktop/Trabalho/Alexandre%20Queiroz%20Marketing%20Digital/DIAX/CRM/crm-web): The official user interface.
*   [`scraper-google-email/`](file:///C:/Users/acq20/Desktop/Trabalho/Alexandre%20Queiroz%20Marketing%20Digital/DIAX/CRM/scraper-google-email): Python-based lead collection tool.
*   [`n8n-workflows/`](file:///C:/Users/acq20/Desktop/Trabalho/Alexandre%20Queiroz%20Marketing%20Digital/DIAX/CRM/n8n-workflows): JSON exports of core business automations.
*   [`infra/`](file:///C:/Users/acq20/Desktop/Trabalho/Alexandre%20Queiroz%20Marketing%20Digital/DIAX/CRM/infra): Infrastructure configurations and setup.
*   [`docs/ai/`](file:///C:/Users/acq20/Desktop/Trabalho/Alexandre%20Queiroz%20Marketing%20Digital/DIAX/CRM/docs/ai): AI-readable documentation (This directory).
