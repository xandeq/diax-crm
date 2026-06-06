# DIAX MCP — Architecture

## Overview

`diax-mcp` is a Model Context Protocol server that exposes the DIAX CRM API to Claude (Claude Code, Claude Desktop, and compatible clients). It runs as a local process via STDIO transport.

```
Claude (client)
    │  STDIO (JSON-RPC 2.0)
    ▼
diax-mcp (Node.js/TypeScript)
    │  HTTPS REST
    ▼
DIAX CRM API (.NET 8)
    │  EF Core
    ▼
SQL Server (SmarterASP.NET)
```

## Project structure

```
diax-mcp/
├── src/
│   └── index.ts          # Single-file server: config, auth, tools, handlers
├── scripts/
│   ├── run_campaign.py               # Bulk email campaign creator (parametrized)
│   └── campaign-config.example.json  # Template — copy to campaign-config.json
├── docs/
│   ├── ARCHITECTURE.md   # This file
│   └── TOOLS.md          # Tool reference
├── dist/                 # Compiled output (gitignored)
├── node_modules/         # Dependencies (gitignored)
├── .env.example          # Environment variable template
├── package.json
└── tsconfig.json
```

## Authentication

Two modes, resolved in priority order:

1. **Service API Key** (`DIAX_SERVICE_API_KEY` env) — header `X-Api-Key`. Preferred for automation.
2. **JWT login** (`DIAX_ADMIN_EMAIL` + `DIAX_ADMIN_PASSWORD`) — `POST /api/v1/auth/login`. Token cached in memory, auto-refreshed 60s before expiry and on 401.

Credentials are read from: `process.env` → `~/.claude/.secrets.env` → fallback empty.

## Configuration

| Variable | Description |
|---|---|
| `DIAX_API_BASE_URL` | API base URL (aliases: `DIAX_API_URL`). Default: `https://api.alexandrequeiroz.com.br` |
| `DIAX_SERVICE_API_KEY` | Service key (preferred over JWT) |
| `DIAX_ADMIN_EMAIL` | Admin email for JWT login |
| `DIAX_ADMIN_PASSWORD` | Admin password for JWT login |

See `.env.example` for a template.

## Build & run

```bash
# Install deps
npm install

# Build TypeScript
npm run build          # → dist/index.js

# Dev (no build required)
npm run dev            # uses tsx

# Production
npm start              # node dist/index.js
```

## Claude integration

Registered in `~/.claude/settings.json`:

```json
{
  "mcpServers": {
    "diax-crm": {
      "command": "node",
      "args": ["D:/claude-code/diax-crm/diax-mcp/dist/index.js"]
    }
  }
}
```

## Error handling

- Network/API errors surface as `isError: true` in the MCP response — Claude sees the message and can retry.
- 401 triggers a single automatic re-login (JWT mode only).
- All errors are logged to `stderr` (visible in Claude Code console, not in Claude's conversation).
