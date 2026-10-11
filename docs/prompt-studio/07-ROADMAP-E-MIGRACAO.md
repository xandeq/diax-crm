# Prompt Studio — Roadmap priorizado e plano de migração

Estimativas em dias ideais de 1 dev com Claude Code. Cada fase termina em PR próprio, deployável e sem quebrar nada (feature visível só quando a fase 2 entra).

## Progresso (2026-10-11)

- [x] **F0** domínio + migração aditiva + renderer + testes (`00ad6570`)
- [x] **F1** API: CRUD, versões/409/no-op, restore, render, tags (+rename), coleções, from-generator/from-snippet (`00ad6570`)
- [x] **F1** import/export `.md/.txt/.zip` com dry-run, estratégias e sabor Claude Code (`c84fdd57` + UI). `.json` adiado.
- [x] **F2** UI `/prompts` integrada ao AppShell: sidebar, lista, filtro local, editor com destaque, variáveis, preview, histórico+diff+restore, rascunho local, conflito (`13a4aa8e`, `8e567926`)
- [x] **F3** nav + paleta global, "Salvar no Studio" no Gerador (resultado e histórico), "Virar prompt" nos Snippets
- [ ] **F3** "Reescrever com framework" (`/rewrite`) — pendente
- [ ] Import dos 5 `prompts/*.txt` em produção — importador pronto e testado com esses arquivos; falta rodar depois do deploy
- [x] **F4** `diax-mcp` 1.1: 4 tools + capability `prompts` (`c8dffd61`)
- [ ] **Gate MVP**: aplicar migração em produção, smoke real com dados, axe-core, roteiro de teclado em produção

## MVP (≈ 9–11 dias): biblioteca útil + Claude Code

| Fase | Entrega | Critérios | Estimativa |
|---|---|---|---|
| **F0 Fundações** | Domínio `PromptLibrary`, configs EF (`nvarchar(max)` explícito), migração aditiva `AddPromptLibrary`, repositórios, `PromptVariableParser` + `PromptRenderer` + fixture de vetores, testes unitários | AC-21, 25, 30–32, 34, 83 | 2 d |
| **F1 API** | `PromptsController` + `PromptCollectionsController`: CRUD, versões, restore, render, tags, use, índice com ETag; import/export `.md`/`.zip`/`.json` com dry-run; `from-generator`, `from-snippet` | AC-03, 05, 06, 12, 22, 50–54, 60–61 | 2,5 d |
| **F2 UI núcleo** | `/prompts`: shell de 3 painéis, sidebar, lista com filtro local, editor CodeMirror (lazy), painel de variáveis, preview, histórico + diff, ConflictDialog, estados vazio/erro, responsivo | AC-01–04, 07, 10–11, 20, 23–24, 33, 84–87 | 3 d |
| **F3 Integrações** | Command palette (local + registro na global do AppShell), item "Prompt Studio" no nav, botão "Salvar no Studio" no Gerador e no histórico, "Converter em prompt" no Snippets, galeria de frameworks, "Reescrever com framework", import dos 5 `.txt` | AC-13–14, 40–42, 60–62 | 1,5 d |
| **F4 MCP** | `diax-mcp` v1.1: capability `prompts` + `search/get/render/save_prompt`; doc de uso no Claude Code; export `flavor=claude-command` | AC-55, 70–74 | 1 d |
| **Gate MVP** | Regressão completa + roteiro manual de teclado/mobile | AC-80–82, 85–86 | 0,5 d |

**Corte se o prazo apertar** (nesta ordem): seleção múltipla em lote → reordenar coleções por drag → `flavor=claude-command` → "Reescrever com framework" (fica só a galeria). Nunca cortar: versionamento, variáveis, palette, MCP e testes de regressão.

## V2 (≈ 8 dias): testar, comparar, avaliar, Git

1. **Refatoração segura** (1,5 d): testes de caracterização do `PromptGeneratorService` → extração do `ITextLlmClient` → o Gerador passa a usá-lo (PR isolado).
2. **Playground** (1,5 d): aba "Testar": variáveis + provider/modelo → saída com streaming simulado, latência, tokens, fallback; histórico de execuções; nota 1–5.
3. **Comparação** (1 d): 2–4 alvos (versão × modelo) lado a lado, mesmo `batchId`, diff das saídas.
4. **Casos de teste + asserções** (2 d): CRUD, rodar a suíte, taxa de aprovação por versão no histórico.
5. **Git sync one-way** (1,5 d): config do repositório, token cifrado, commit via GitHub Trees API, botão "Sincronizar" + última sincronização.
6. Extras: widget "Mais usados" no dashboard; atualizar `DESIGN.md` para refletir o `globals.css` real.

## V3 (sob demanda, por valor medido)
- LLM-as-judge com rubrica por prompt (custo controlado: provider grátis primeiro, regra de auto-fallback).
- Git bidirecional (pull com detecção de conflito por hash).
- Analytics: uso por canal, prompts mortos (> 90 dias sem uso), versões com melhor nota.
- Link público somente leitura (padrão do Snippet público) e export para Notion.
- Chains (prompt A → saída vira variável de B) se o uso real pedir.
- Tema claro (decisão global do CRM).

## Plano de migração (sem quebrar nada)

| Origem | Estratégia | Quando | Reversível? |
|---|---|---|---|
| `user_prompts` (histórico do Gerador) | **Opt-in**: botão "Salvar no Studio" em cada resultado/linha do histórico + "Importar do histórico" com multi-seleção (`from-generator/bulk`). Sem migração automática em massa, para não poluir a biblioteca com rascunhos | F3 | Sim: a tabela não muda; os prompts criados guardam `source_ref_id` |
| Snippets `text`/`markdown` | Ação "Converter em prompt" na lista do Snippets; o Snippet permanece (continua servindo como pastebin com link público e anexos) | F3 | Sim |
| `diax-crm/prompts/*.txt` (5) | Import `.md`/`.txt` → coleção "Dev"; os arquivos continuam no repo até a validação, depois são removidos num PR à parte | F3 | Sim |
| Commands/skills do Claude Code que forem prompts puros | Import de `.md` sem bloco `diax` (AC-51) quando o usuário quiser; **não** importar skills com scripts | Pós-MVP | Sim |
| Frameworks (`promptTypeOptions` + meta-prompts) | Ficam onde estão (fonte única). O Studio referencia pela chave; nenhum meta-prompt é duplicado | — | n/a |
| `RecentPromptsWidget` | Inalterado | — | n/a |

**Ordem de deploy:** F0+F1 (API invisível, migração aditiva aplicada no startup) → F2+F3 (UI liberada no nav) → F4 (MCP publicado localmente; `claude mcp` já aponta para o `diax-mcp`). Rollback de cada fase = revert do PR; as tabelas novas podem ficar vazias sem efeito colateral.

## Próximos passos imediatos
1. Revisar e aprovar este pacote (PRD, ADRs, protótipo).
2. Abrir `F0` na própria `feat/prompt-studio` (ou em sub-branches `feat/prompt-studio-f0…` para PRs menores).
3. Rodar `npm view @uiw/react-codemirror version` e `dotnet add package YamlDotNet` para validar as versões compatíveis com Next 16 e .NET 10 antes de começar.
