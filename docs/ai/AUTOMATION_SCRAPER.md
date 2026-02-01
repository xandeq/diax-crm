# DIAX CRM - Automation & Scraper

This component manages the automated acquisition of leads and high-level business workflows.

## 🕵️ Lead Scraper (`scraper-google-email/`)

A Python application using **Selenium** and **BeautifulSoup** to gather lead data.

### Core Logic (`main.py`)
*   Supports two modes: **Google Search** (`-q`) or **Direct Site Crawling** (`-s`).
*   **Search**: Parses search results for relevant domains.
*   **Crawl**: Deeply analyzes a single domain for emails, phone numbers, and company info using recursive link analysis.
*   **Storage**: Outputs results to CSV files in the `output/` folder.

### Key Dependencies
*   `selenium`, `beautifulsoup4`, `webdriver-manager`.

## ⚙️ n8n Workflows (`n8n-workflows/`)

Orchestration layer for complex interactions.
*   **`whatsapp-routing.json`**: Logic for managing incoming/outgoing messages and CRM routing.
*   **`n8n-workflow.json`**: General data sync and notification workflows.
*   Integrates with API Core via REST for updating lead statuses based on external events.

## 💡 Future Extensibility
The scraper and workflows are modular. New "sources" for leads or new communication channels (e.g., Telegram, Slack) can be added by implementing new specific scraper modules or n8n nodes that target the `API Core` endpoints.
