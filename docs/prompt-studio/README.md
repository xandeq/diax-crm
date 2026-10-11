# Prompt Studio (DIAX CRM)

Biblioteca central de prompts do DIAX CRM: coleções, tags, favoritos, busca, editor Markdown com variáveis, versionamento com diff, import/export compatível com Claude Code e integração MCP. V2 traz testes, comparação, avaliações e Git sync.

> **Etapa atual:** especificação, arquitetura, design e roadmap. **Nenhum código do módulo foi implementado.**
> Branch `feat/prompt-studio` (sem upstream, para evitar push acidental na `main`) · base `origin/main` @ `aa586e54`.

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
Aprovar o pacote → iniciar a **F0 (fundações: domínio, migração aditiva, renderer com fixture de vetores)** nesta branch.
