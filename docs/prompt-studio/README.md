# Prompt Studio (DIAX CRM)

Biblioteca central de prompts do DIAX CRM: coleções, tags, favoritos, busca, editor Markdown com variáveis, versionamento com diff, import/export compatível com Claude Code e integração MCP. V2 traz testes, comparação, avaliações e Git sync.

> **Etapa atual:** MVP implementado (F0–F4) na branch `feat/prompt-studio`, **sem push/merge e com migração NÃO aplicada em produção**.
> Branch sem upstream (evita push acidental na `main`) · base `origin/main` @ `aa586e54`.

## Status da implementação (2026-10-11)

| Fase | Status | Commit | Evidência |
|---|---|---|---|
| Specs + protótipo | ✅ | `c8a935ab` | este diretório |
| F0+F1 backend (domínio, migração aditiva, API) | ✅ | `00ad6570` | 42 testes verdes (`dotnet test --filter PromptLibrary`) |
| F2+F3 UI `/prompts` + Gerador + Snippets | ✅ | `13a4aa8e`, `8e567926` | `tsc` limpo, `next build` ok, revisão no navegador contra API mock |
| F4 MCP (`diax-mcp`) | ✅ | `c8dffd61` | smoke e2e com cliente MCP real (tools, prompts/list, prompts/get) |
| Import/export `.md/.zip/.json` | ⏳ pendente | — | contrato em 04 §5 |
| "Reescrever com framework" (`/rewrite`) | ⏳ pendente | — | contrato em 04 §2 |
| V2 (playground, comparação, testes, avaliações, Git) | ⏳ | — | 07 |

**Antes de ir para produção:** revisar o PR → aplicar `AddPromptLibrary` (só cria 4 tabelas; o `Migrate()` do startup aplica sozinho no deploy) → publicar `diax-mcp` 1.1.

### Desvios das specs decididos na implementação
- **Editor:** textarea com camada de destaque (zero dependências, undo nativo) em vez de CodeMirror. CodeMirror fica para quando faltar autocomplete de `{{`. (ADR-05 revisado)
- **Paleta:** o `Ctrl+K` já pertence à paleta global do AppShell → o Studio não cria outra; ganhou entradas na global e atalhos locais (`/`, `J/K`, `F`, `E`, `N`, `Ctrl+S`, `Ctrl+Enter`, `Ctrl+1..3`).
- **Lista:** `GET /prompts` devolve o índice inteiro (até 2.000, sem paginação) e o filtro fino é local; `q` existe para busca no conteúdo pelo servidor/MCP.
- **Metadados:** `PATCH` sem `If-Match/rowVersion` (single-user, last-write-wins); conflito de **conteúdo** continua protegido pelo `baseVersionNumber` → 409.
- **Variáveis persistidas** = as presentes no conteúdo (declaradas e não usadas são descartadas, sem warning).
- **Copiar** fica no topo do painel de variáveis (o toast global do CRM cobria o botão no rodapé).

## Documentos

| # | Documento | Conteúdo |
|---|---|---|
| 1 | [01-PRD.md](./01-PRD.md) | Problema, objetivos e métricas, persona/JTBD, escopo P0/P1/P2, fluxos, requisitos não funcionais |
| 2 | [02-ARQUITETURA.md](./02-ARQUITETURA.md) | Auditoria do existente, visão geral, **12 ADRs**, organização de código, dependências, segurança, **riscos de integração** |
| 3 | [03-MODELO-DE-DADOS.md](./03-MODELO-DE-DADOS.md) | Tabelas MVP/V2, índices, invariantes de domínio, migração aditiva |
| 4 | [04-CONTRATOS-API.md](./04-CONTRATOS-API.md) | REST (`/prompts`, `/prompt-collections`, import/export, origens, V2) e MCP (tools + capability `prompts`) |
| 5 | [05-DESIGN-SYSTEM.md](./05-DESIGN-SYSTEM.md) | Princípios, tokens (sobre o `globals.css` real), layout responsivo, anatomia das telas, atalhos, estados, a11y, reuso de componentes |
| 6 | [06-CRITERIOS-DE-ACEITE.md](./06-CRITERIOS-DE-ACEITE.md) | ~50 critérios Dado/Quando/Então, marcados [auto] quando viram teste |
| 7 | [07-ROADMAP-E-MIGRACAO.md](./07-ROADMAP-E-MIGRACAO.md) | Fases MVP (F0–F4), V2, V3, plano de migração sem quebra, ordem de deploy |
| — | [prototype/prompt-studio.html](./prototype/prompt-studio.html) | Protótipo navegável (abrir no navegador): lista, filtro, editor com destaque de variáveis, painel de variáveis com render ao vivo, histórico com diff e restaurar, paleta `Ctrl+K`, atalhos, responsivo |

## Decisões-chave (resumo)

1. **Módulo novo `PromptLibrary`**: Gerador, Snippets e `user_prompts` não mudam de schema (ADR-01). Migração EF 100% aditiva.
2. **Versões imutáveis em snapshot**, restaurar = nova versão, conflito web × MCP → 409 (ADR-02, ADR-12).
3. **Rota única `/prompts/?p=<id>`**, por causa do static export (ADR-03).
4. **Variáveis `{{nome}}`**, a mesma convenção do email worker, com render idêntico no cliente e no servidor (ADR-04).
5. **CodeMirror 6** (lazy) em vez de Tiptap: prompt é texto exato (ADR-05).
6. **Busca local + `LIKE`**, sem full-text no SQL compartilhado (ADR-06).
7. **Arquivo canônico `.md` + frontmatter compatível com Claude Code** (ADR-08).
8. **Design segue `globals.css` (dark verde)**; o `DESIGN.md` está desatualizado (ADR-09).
9. **MCP como cliente fino da API**: `search/get/render/save_prompt` + prompts como `/mcp__diax-crm__<slug>` (ADR-10).
10. **Playground (V2) reusa a pilha de providers** extraindo `ITextLlmClient` com testes de caracterização antes (ADR-11).

## Próxima ação recomendada
Revisar os 5 commits e abrir PR (fluxo do repo: worktree → PR → squash). Depois: import/export `.md` (fecha o ciclo com os commands do Claude Code) e importar os 5 `prompts/*.txt`.
