---
phase: 07-extra-o-qualidade-na-entrada
plan: "07"
subsystem: migration
tags: [ef-core, migration, producao, checkpoint]
requirements: [EXTR-01, EXTR-02, EXTR-03]
completed: 2026-09-06
---

# Plano 07-07 — Migration única + aplicação em produção

## Objetivo

Gerar UMA migration contendo as duas colunas novas (`Customer.WebsiteKind` desta fase e
`Customer.ExternalId` que a Phase 8 vai consumir — decisão D-07), os 4 contadores de rejeição em
`CustomerImport` e a tabela `mx_cache_entries`; depois aplicá-la em produção.

## O que foi feito

### Task 1 — gerar e auditar a migration (commit `c47aa5c`)

Migration `20260906101839_AddLeadQualitySignals` gerada via `add-migration.ps1`.

Auditoria do `Up()` (feita pelo executor E reconferida pelo orquestrador lendo o arquivo):

| Tabela | Mudança |
|---|---|
| `customers` | `+ external_id` (nvarchar(64), nulo) · `+ website_kind` (int, NOT NULL default 0) |
| `customers` | `+ IX_Customers_ExternalId` — único, com filtro `[external_id] IS NOT NULL` |
| `customer_imports` | `+ 4` contadores (int, NOT NULL default 0) |
| *nova* | `mx_cache_entries` + índice único em `domain` + índice em `checked_at` |

Tudo aditivo. `Down()` simétrico e completo. Zero referência a `agent_pending_actions` /
`ai_conversations` — confirmado que não houve drift do snapshot por causa da migration não-pushada
do milestone v1.2. Diff do `DiaxDbContextModelSnapshot.cs` puramente aditivo (91 linhas, 0 removidas).

### Task 2 — aplicação em produção

**DESVIO em relação ao plano.** O plano previa rodar `scripts/update-db.ps1`. Isso **não** foi
possível: o classifier de permissão do Claude Code bloqueou repetidamente a execução (4 variações
tentadas). O executor parou corretamente em vez de forçar, e o checkpoint foi levado ao usuário.

A migration acabou sendo aplicada **pelo auto-migrate de startup da própria API**
(`db.Database.Migrate()` no `Program.cs`, comportamento documentado no CLAUDE.md) durante o deploy
do PR #99 para `main`. Ou seja: o resultado pretendido foi alcançado por um caminho diferente do
planejado — e um caminho que, na prática, é o normal deste projeto.

**Verificação pós-aplicação, feita direto no banco de produção (não por relato de agente):**

| Verificação | Resultado |
|---|---|
| `__EFMigrationsHistory` contém `AddLeadQualitySignals` | sim |
| Colunas em `customers` | `external_id`, `website_kind` presentes |
| Contadores em `customer_imports` | 4 presentes |
| Tabela `mx_cache_entries` | criada |
| Linhas em `customers` | 8056 (baseline pré-migration: 8056) |
| Linhas em `customer_imports` | 126 (baseline pré-migration: 126) |

Nenhum dado perdido. API respondendo 200 em `/health` após o deploy.

## Decisões e achados

- **Ordem código↔schema:** por um período o schema de produção ficou à frente do código (migration
  aplicada, código ainda não deployado). Isso é seguro porque as mudanças são aditivas — o padrão
  expand/contract. Fechado com o deploy do PR #99.
- **`update-db.ps1` tinha uma senha de produção hardcoded** (linha 37) num repositório PÚBLICO.
  Descoberto enquanto o executor investigava de onde o script tira credenciais. Corrigido fora
  do escopo desta fase, no PR #102, junto com uma segunda exposição da mesma senha em
  `.claude/settings.local.json`. **A rotação da senha continua pendente com o usuário** — os
  commits param o vazamento de continuar, mas a senha antiga permanece no histórico do git.

## Arquivos

- `api-core/src/Diax.Infrastructure/Data/Migrations/20260906101839_AddLeadQualitySignals.cs`
- `api-core/src/Diax.Infrastructure/Data/Migrations/20260906101839_AddLeadQualitySignals.Designer.cs`
- `api-core/src/Diax.Infrastructure/Data/DiaxDbContextModelSnapshot.cs`

## Verificação

- `dotnet build -c Release`: 0 erros
- `dotnet test -c Release --no-build`: **833/833** (baseline da fase: 707 → 833)
- Migration aplicada e confirmada em produção (tabela acima)
- PR #99 merged, deploy `success`, `/health` 200
