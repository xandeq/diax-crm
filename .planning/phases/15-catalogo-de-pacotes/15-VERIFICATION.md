---
phase: 15-catalogo-de-pacotes
verified: null
status: pending
score: null
---

# Phase 15: Catálogo de Pacotes — Verification Plan (not yet run)

**Phase Goal:** O catálogo de pacotes de serviço (preço fixo) existe, é gerenciável via API, e
`Proposal` está pronta para linkar a um pacote e rastrear follow-up — fundação sem qualquer impacto
visível ainda no app em produção.

**Status:** ⏳ Pendente — executar quando o PR-a (branch `feat/service-packages`, worktree
`_worktrees/diax-pr-a`) existir e CI reportar. O gerente chama este agente (ou outro) para rodar a
verificação então; este arquivo só registra os critérios goal-backward decididos agora.

**Plano fonte:** `docs/plans/2026-10-23-fechar-venda.md`, Tasks 1-4. Design:
`docs/plans/2026-10-23-fechar-venda-design.md`.

## Goal Achievement — critérios a checar (goal-backward)

| # | Truth (precisa ser verdade) | Como verificar | Evidência esperada |
|---|------------------------------|-----------------|---------------------|
| 1 | `ServicePackage` existe com os campos do design (`Name`, `ShortDescription`, `Scope`, `Price`, `DeliveryDays`, `IsActive`, `SortOrder`) e rejeita nome vazio/preço ≤0/prazo negativo | Ler `api-core/src/Diax.Domain/Customers/ServicePackage.cs`; rodar `dotnet test --filter "FullyQualifiedName~ServicePackageTests"` | Classe implementa `AuditableEntity`/`IUserOwnedEntity`; construtor + `Update` lançam `ArgumentException` nos 3 casos inválidos; 5 testes do plano passam |
| 2 | `ToProposalDescription()` monta bullets do escopo + prazo por extenso | Ler o método; rodar o teste `ToProposalDescription_ListsScopeAndDeadline` | Saída contém `• <item>` por linha do `Scope` e `"<N> dias"` quando `DeliveryDays > 0` |
| 3 | `Proposal` ganha `PackageId`/`Origin`/`LastFollowUpStage` sem quebrar nada existente | Ler `Proposal.cs` (diff); rodar `dotnet test --filter "FullyQualifiedName~Proposal"` (suíte inteira de Proposal, não só a nova classe) | `LinkPackage`/`SetOrigin`/`RegisterFollowUpStage`/`IsOpen` presentes; `RegisterFollowUpStage` só avança (nunca retrocede, nunca repete); testes antigos de `Proposal` continuam verdes |
| 4 | Migration única, sem tocar outras tabelas | Ler o arquivo gerado em `api-core/src/Diax.Infrastructure/Data/Migrations/<ts>_AddServicePackages.cs` linha a linha | Só `CreateTable("service_packages")` + até 3 `AddColumn` em `proposals` (`package_id` nullable, `origin` default 0, `last_follow_up_stage` default 0) + `CreateIndex`. **Zero** `DropTable`/`DropColumn`/`AlterColumn` de qualquer outra tabela. Se houver, é bloqueante — não aprovar merge. |
| 5 | CRUD de pacotes funciona via API, isolado por usuário | Ler `ServicePackageService.cs` + `ServicePackagesController.cs`; rodar `dotnet test --filter "FullyQualifiedName~ServicePackageServiceTests"` | `CreateAsync` rejeita inválido sem salvar (`_uow.SaveChangesAsync` `Times.Never`); `UpdateAsync` de pacote de outro usuário devolve `NotFound`; `GET /service-packages?onlyActive=` filtra por `UserId` |
| 6 | Build e suíte completa .NET passam, nada quebrou | `dotnet build api-core/Diax.CRM.sln -c Release` + `dotnet test api-core/tests/Diax.Tests/Diax.Tests.csproj` | 0 erros de build; suíte inteira verde (comparar contagem de testes antes/depois — só deve crescer) |
| 7 | PR-a aberto, CI 6/6, sem merge sem ok | Checar o PR no GitHub (`gh pr view` ou API) | Checks verdes; branch não mergeada em `main` sem confirmação explícita registrada em `STATE.md`/pelo usuário |

## Sinais de desvio a observar (do plan-check desta sessão)

- `IRepository<T>.AddAsync` retorna `Task<T>` (não `Task`) — se o `Setup`/`Verify` do teste de
  `ServicePackageServiceTests` não bater com essa assinatura, é erro de implementação, não do
  contrato (confirmado em `api-core/src/Diax.Domain/Common/IRepository.cs`).
- `IUserOwnedEntity.UserId` é `{ get; }` — `ServicePackage.UserId` pode (e deve, como `Proposal`)
  declarar `{ get; set; }`; isso satisfaz a interface normalmente, não é um erro a corrigir.
- Linhas de referência do plano (`DiaxDbContext.cs:75`, `DependencyInjection.cs:274`/`559`,
  `Application/DependencyInjection.cs:64`, `Program.cs:353`) foram confirmadas EXATAS contra o
  código real nesta revisão (2026-10-24) — se o diff final não tocar essas linhas nesses arquivos,
  investigar por quê antes de aprovar.

## Requirements Coverage (a preencher na verificação real)

| Requirement | Status | Evidência |
|-------------|--------|-----------|
| PKG-01 | Pending | — |
| PKG-02 | Pending | — |

---
*Preparado por gsd-pm em 2026-10-24, antes da execução da Phase 15. Rodar a verificação de verdade
só quando o PR-a existir — ver `STATE.md` para o status corrente.*
