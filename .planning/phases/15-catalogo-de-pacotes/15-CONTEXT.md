# Phase 15 Context — Catálogo de Pacotes

**Milestone:** v1.5 Fechar Venda
**Status:** Em execução por **superpowers-dev**, worktree `D:\claude-code\_worktrees\diax-pr-a`,
branch `feat/service-packages` (PR-a quando aberto).

## Esta fase não tem PLAN.md GSD

O plano já existe fora do formato GSD, aprovado pelo usuário antes deste roadmap ser criado:

- Design: `docs/plans/2026-10-23-fechar-venda-design.md`
- Plano de implementação (TDD, task-by-task): `docs/plans/2026-10-23-fechar-venda.md`, **Tasks 1-4**

Quem for executar ou continuar esta fase deve seguir esse plano diretamente — este arquivo só
mapeia a fase para o GSD (requirements, goal, verificação), não duplica as instruções de código.

## Goal

O catálogo de pacotes de serviço (preço fixo) existe, é gerenciável via API, e `Proposal` está
pronta para linkar a um pacote e rastrear follow-up — fundação sem qualquer impacto visível ainda
no app em produção.

## Requirements

PKG-01 (catálogo `service_packages` com CRUD via API), PKG-02 (`Proposal.PackageId`/`Origin`/
`LastFollowUpStage`).

## Decisões já travadas (do design, não reabrir)

- `service_packages`: `name`, `short_description`, `scope` (itens, um por linha), `price`,
  `delivery_days`, `is_active`, `sort_order`, auditoria padrão (`AuditableEntity`).
- `ServicePackage.ToProposalDescription()` monta a descrição da proposta a partir do escopo (bullets
  `• item`) + prazo ("Prazo de entrega: N dias úteis após aprovação.").
- `Proposal` ganha `PackageId` (opcional, FK), `Origin` (enum `Manual`/`Bot`, default `Manual`) e
  `LastFollowUpStage` (int, default 0, só avança via `RegisterFollowUpStage`).
- Migration única para esta fase: `CreateTable("service_packages")` + 3 `AddColumn` em `proposals`
  (`package_id` nullable, `origin` default 0, `last_follow_up_stage` default 0) + índices. **Nunca**
  roda `update-db.ps1` nesta fase — a migration só é gerada (`add-migration.ps1`) e vai para produção
  via auto-migrate do startup depois do merge na `main`.
- Começa com 3-4 pacotes aprovados pelo usuário (sugestão inicial de preço: R$1.800-3.200, a partir
  das propostas de julho) — cadastro de dados, não faz parte do código desta fase.

## Dependências

Nenhuma — reaproveita `Proposal` (`Diax.Domain/Customers/Proposal.cs`) e o padrão de repositório/
serviço/controller já existentes (`ProposalsController`, `IUnitOfWork`, `IRepository<T>`). Não altera
nenhum endpoint ou comportamento existente.

## Depois desta fase

Phase 16 (Proposta a Partir de Pacote, PR-b) depende do catálogo e dos campos novos de `Proposal`
desta fase. Verificação: `15-VERIFICATION.md` nesta pasta — o gerente/gsd-pm roda quando o PR-a
existir.
