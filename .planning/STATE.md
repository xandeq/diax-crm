# State — DIAX CRM

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-03)

**Core value:** Centralizar todas as operações de negócio em um único sistema pessoal
**Current focus:** Definindo requirements para v1.1

## Current Position

Phase: Not started (defining requirements)
Plan: —
Status: Defining requirements
Last activity: 2026-04-03 — Milestone v1.1 iniciado, GSD planning inicializado

## Accumulated Context

- Sistema em produção em crm.alexandrequeiroz.com.br
- Backend: api.alexandrequeiroz.com.br (SmarterASP.NET)
- DB: SQL Server `db_aaf0a8_diaxcrm` em `sql1002.site4now.net`
- Deploy automático via GitHub Actions (push to main)
- Usar sempre `update-db.ps1` para migrations (nunca dotnet ef direto)
- Frontend é static export — sem SSR
