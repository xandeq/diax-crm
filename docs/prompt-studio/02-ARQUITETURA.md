# Prompt Studio — Arquitetura, decisões e riscos

## 1. Auditoria do que existe (base para as decisões)

| Ativo | Local | Achado | Reuso |
|---|---|---|---|
| Gerador de Prompts | `PromptGeneratorController` → `PromptGeneratorService` (1.590 linhas) | 20 meta-prompts hardcoded (`BuildXxxMetaPrompt`), fallback entre 6 providers, envio HTTP privado dentro do service | Chamar `POST /prompt-generator/generate` para "Reescrever com framework" (MVP). Extrair `ITextLlmClient` só na V2 (Playground) |
| Histórico do gerador | Tabela `user_prompts` (`UserPrompt`): input, output, tipo, provider, modelo | Só `GET history` / `GET history/{id}` | Fonte de migração opt-in (`from-generator/{id}`); a tabela não muda |
| Lista de frameworks (UI) | `crm-web/src/services/promptGenerator.ts` → `promptTypeOptions` | Descrição, quando usar e exemplo de cada um | Fonte única da galeria de frameworks no Studio |
| Snippets | `SnippetsController`, entidade `Snippet` + anexos em disco | Create/Get/Delete, link público, **sem update** | Ação "Converter em prompt"; o módulo continua como pastebin |
| Widget | `RecentPromptsWidget` (dashboard) | Lê o histórico do gerador | Inalterado (V2: opção de mostrar os mais usados do Studio) |
| MCP | `diax-mcp/src/index.ts`: `Server` de baixo nível, `capabilities: { tools: {} }`, 16 tools, login JWT com cache | Sem `prompts` capability | Adicionar 4 tools e a capability `prompts` |
| Navegação | `AppShell.tsx`: sidebar com seção "IA" e command palette com atalho | — | Novo item "Prompt Studio" na seção IA e entradas na palette global |
| Design real | `crm-web/src/app/globals.css`: dark-first, `--diax-bg #0F1A14`, verde `#10B981`, Plus Jakarta Sans, `--radius .625rem` | `DESIGN.md` (teal, light-first) está **desatualizado** em relação ao código | O Studio segue `globals.css` (ver ADR-09) |
| Rotas dinâmicas | `campanhas/[id]` usa `generateStaticParams` com placeholder | Refresh em deep link depende de rewrite no host | Studio usa rota única + query string (ADR-03) |
| `prompts/*.txt` | Raiz do repo | 5 prompts de dev | Importados no seed da coleção "Dev" |

## 2. Visão geral

```
┌──────────────── crm-web (Next 16, static export) ────────────────┐
│ /prompts/?p=<id>&v=<n>&tab=editor|history|test                   │
│  ├─ PromptStudioShell (3 painéis)                                 │
│  ├─ PromptEditor (CodeMirror 6, lazy)  ├─ VariablesPanel          │
│  ├─ VersionHistory + DiffView (jsdiff) ├─ CommandBar (Ctrl+K)     │
│  └─ services/promptStudio.ts (apiFetch)                          │
└───────────────┬──────────────────────────────────────────────────┘
                │ REST /api/v1/prompts*, /prompt-collections
┌───────────────▼──────────── api-core (.NET 10) ──────────────────┐
│ Api: PromptsController, PromptCollectionsController               │
│ Application/PromptLibrary: PromptLibraryService,                  │
│   PromptRenderer, PromptMarkdownSerializer, PromptImportService   │
│ Domain/PromptLibrary: Prompt, PromptVersion, PromptTag,           │
│   PromptCollection (+ V2: PromptRun, PromptTestCase, PromptEval)  │
│ Infrastructure: EF configs + migração aditiva                     │
└───────────────▲──────────────────────────────────────────────────┘
                │ mesma API REST (JWT)
┌───────────────┴──── diax-mcp (stdio) ─── Claude Code ────────────┐
│ tools: search_prompts, get_prompt, render_prompt, save_prompt     │
│ prompts: ListPrompts (expose_to_mcp=1) / GetPrompt → render       │
│ → /mcp__diax-crm__<slug> no Claude Code                           │
└──────────────────────────────────────────────────────────────────┘
```

O backend é a fonte da verdade. Web, MCP e import/export falam com a mesma API e as mesmas regras de versão. Não existe caminho de escrita paralelo.

## 3. Decisões arquiteturais (ADRs)

### ADR-01: módulo novo `PromptLibrary`, sem alterar Gerador nem Snippets
- **Decisão:** bounded context novo (`Domain/PromptLibrary`, `Application/PromptLibrary`, controllers próprios). As integrações são feitas por ações explícitas (salvar, converter), nunca por mudança de schema nas tabelas existentes.
- **Por quê:** zero risco de regressão em módulos em produção; a migração EF é 100% aditiva (o CRM aplica `Migrate()` no startup em produção).
- **Alternativa descartada:** evoluir `Snippet` (teria que adicionar update, tags, versões e variáveis, e misturar pastebin com biblioteca) ou evoluir `UserPrompt` (é um log de geração, não um documento editável).

### ADR-02: versões imutáveis em snapshot completo
- **Decisão:** `PromptVersion` guarda o conteúdo inteiro, numerado 1..n por prompt; `Prompt.CurrentVersionNumber` aponta a atual. Restaurar = criar versão nova com o conteúdo antigo. Save com o mesmo `ContentHash` (SHA-256) = no-op.
- **Por quê:** prompts são textos pequenos (< 20 KB), então snapshot é barato, simples e auditável. O diff é calculado no cliente (`diff`/jsdiff).
- **Descartado:** deltas/patches (complexo sem ganho) e CRDT/colaboração em tempo real (single-user).

### ADR-03: rota única `/prompts/` com estado na query string
- **Decisão:** `/prompts/?p=<id>&v=<n>&tab=history`. Estado de seleção via `useSearchParams` + `router.replace`.
- **Por quê:** `output: 'export'` não gera rotas dinâmicas reais; o padrão placeholder de `campanhas/[id]` quebra o refresh sem regra de rewrite no Hostgator. Query string funciona em qualquer host estático e o link continua compartilhável.

### ADR-04: sintaxe de variável `{{nome}}` + definição opcional tipada
- **Decisão:** placeholders `{{nome}}` (regex `\{\{\s*([a-zA-Z_][\w.-]{0,63})\s*\}\}`). Metadados opcionais por versão em `VariablesJson`: `[{name,label,type:text|textarea|select|number,default,required,options[]}]`. Variável detectada sem definição = `text` obrigatória. Escape literal: `\{{`.
- **Por quê:** mesma convenção do worker de email (`{{FirstName}}`), legível em qualquer LLM e fácil de mapear para argumentos MCP e `$ARGUMENTS` no Claude Code.
- **Render:** função pura idêntica no cliente (preview ao vivo) e no servidor (`/render`, MCP). Testada com os mesmos vetores nos dois lados (fixture JSON compartilhada).

### ADR-05 (revisado na implementação): textarea com overlay de destaque no MVP; CodeMirror 6 depois, nunca Tiptap
> **Implementado:** `TemplateEditor.tsx`: `<textarea>` transparente sobre um `<pre>` com os mesmos métricas, destacando `{{var}}`. Zero dependência nova, undo/IME/acessibilidade nativos, Tab = 2 espaços, Esc libera o foco. Migrar para CodeMirror quando houver demanda real de autocomplete/busca no editor.

**Proposta original:**
- **Decisão:** `@uiw/react-codemirror` + `@codemirror/lang-markdown`, com decoração para `{{var}}` e autocomplete ao digitar `{{`. Carregado com `next/dynamic` só na rota do Studio. Preview com `react-markdown` (já instalado) sem `rehype-raw`.
- **Por quê:** prompt é texto puro em que cada caractere importa. Tiptap (já no projeto, usado no blog) é WYSIWYG e reescreve markdown. CodeMirror tem undo, busca, multi-cursor e boa acessibilidade.
- **Custo:** cerca de 150 KB gz, só nessa rota.

### ADR-06: busca em duas camadas sem full-text
- **Decisão:** (1) o cliente carrega o índice leve (id, título, slug, descrição, tags, preview de 280 chars, coleção, favorito, uso) via `GET /prompts?fields=index` e filtra localmente com ranking simples (título > tag > descrição > preview), instantâneo; (2) a busca no conteúdo completo usa `GET /prompts?q=` com `LIKE` em título, descrição, tags e conteúdo da versão atual.
- **Por quê:** SmarterASP é SQL compartilhado; Full-Text Search não é garantido. Com < 10 mil linhas, `LIKE` com `TOP` fica em ms.
- **Gatilho de revisão:** p95 de busca > 300 ms ou > 10 mil prompts → avaliar FTS ou índice no cliente (MiniSearch).

### ADR-07: tags como tabela-join simples (`PromptTag(PromptId, Name)`)
- **Decisão:** sem entidade Tag com ID. O nome é normalizado (`trim`, minúsculas, `-` no lugar de espaço, máximo 40). Renomear tag = `UPDATE` em lote via endpoint. A cor vem de um hash determinístico do nome.
- **Por quê:** é o menor modelo que atende filtro, contagem e autocomplete. Uma entidade Tag só se paga com metadados por tag (V3, se necessário).

### ADR-08: formato canônico de arquivo = Markdown + frontmatter YAML compatível com Claude Code
- **Decisão:** um prompt = um `.md`. O frontmatter traz `description` (lido pelo Claude Code), `argument-hint` e os campos `diax:` próprios. É o mesmo formato para export, import, Git sync e backup. YAML parseado com YamlDotNet em DTO tipado (sem polimorfismo).
- **Por quê:** o arquivo exportado pode ser colocado direto em `~/.claude/commands/` e vira slash command, sem conversor.

### ADR-09: design segue `globals.css` (dark-first), não o `DESIGN.md`
- **Decisão:** usar os tokens `--diax-*` existentes e acrescentar só os tokens semânticos do Studio (`--ps-*`) como aliases. Atualizar `DESIGN.md` é uma tarefa separada, sugerida no roadmap.
- **Por quê:** o código em produção é a referência real; dois sistemas visuais quebram a consistência.

### ADR-10: MCP = cliente fino da API REST
- **Decisão:** `diax-mcp` ganha `capabilities.prompts` + handlers `ListPrompts`/`GetPrompt` e 4 tools, todos chamando `/api/v1/prompts*`. A lista exposta são os prompts com `expose_to_mcp = true` (máximo 100), com `name = slug` e `arguments` = variáveis.
- **Por quê:** reusa auth e cache JWT já existentes; a regra de render e de versão continua num lugar só (backend).
- **Limite conhecido:** o Claude Code carrega a lista de prompts na conexão. Um prompt novo exposto aparece após `/mcp` reconnect ou reinício. Mitigação: as tools `search/get/render` funcionam sempre, sem depender da lista.

### ADR-11: Playground/execução reusa a pilha de providers (V2), via refatoração com rede de testes
- **Decisão:** extrair de `PromptGeneratorService` um `ITextLlmClient.SendAsync(system, user, provider, model, temperature)` com o mesmo fallback, e fazer o Gerador passar a usá-lo. **Antes:** testes de caracterização do Gerador (payload por provider, ordem de fallback, mapeamento de erro).
- **Por quê:** evita uma segunda implementação de chamadas a LLM e respeita a regra de auto-fallback de provider.

### ADR-12: concorrência otimista
- **Decisão:** `POST /versions` exige `baseVersionNumber`; se diferente da atual → `409 Conflict` com a versão atual no corpo. O `PATCH` de metadados usa `rowVersion`. A UI mostra o diff "sua versão × atual" e oferece salvar como nova versão por cima.
- **Por quê:** web e MCP podem editar o mesmo prompt.

## 4. Organização de código proposta

```
api-core/src/
  Diax.Domain/PromptLibrary/        Prompt.cs, PromptVersion.cs, PromptTag.cs,
                                    PromptCollection.cs, PromptSource.cs, PromptKind.cs,
                                    IPromptRepository.cs, IPromptCollectionRepository.cs
  Diax.Application/PromptLibrary/   PromptLibraryService.cs, PromptRenderer.cs,
                                    PromptVariableParser.cs, PromptMarkdownSerializer.cs,
                                    PromptImportService.cs, Dtos/*
  Diax.Infrastructure/Data/Configurations/Prompt*Configuration.cs
  Diax.Infrastructure/Data/Repositories/Prompt*Repository.cs
  Diax.Api/Controllers/V1/PromptsController.cs, PromptCollectionsController.cs
api-core/tests/Diax.UnitTests/PromptLibrary/   (renderer, parser, serializer, versionamento)
crm-web/src/
  app/prompts/page.tsx                          (Suspense + client shell)
  components/prompt-studio/*                    (Shell, Sidebar, List, Editor, VariablesPanel,
                                                 VersionHistory, DiffView, CommandBar, ImportDialog)
  services/promptStudio.ts
  types/promptStudio.ts
  lib/promptTemplate.ts                         (parse/render, espelho do PromptRenderer)
diax-mcp/src/index.ts                           (+ prompts capability + 4 tools)
```

## 5. Dependências novas

| Pacote | Onde | Motivo | Peso/risco |
|---|---|---|---|
| `@uiw/react-codemirror`, `@codemirror/lang-markdown`, `@codemirror/autocomplete` | crm-web | Editor (ADR-05) | ~150 KB gz, lazy |
| `diff` (jsdiff) | crm-web | Diff de versões | ~10 KB gz |
| `jszip` | crm-web (opcional) | Import `.zip` no cliente | Evitável: o servidor já lê zip; manter só no servidor |
| `YamlDotNet` | Diax.Application | Frontmatter | Madura; deserializar em DTO fechado |
| `System.IO.Compression` | .NET (BCL) | Export/import `.zip` | Nativo |

Nada novo no `diax-mcp` (o SDK 1.29 já suporta prompts).

## 6. Segurança

- Todas as entidades novas implementam `IUserOwnedEntity` (filtro global por usuário), e os controllers usam `[Authorize]`.
- Import: `.md` ≤ 512 KB, `.zip` ≤ 5 MB, ≤ 500 entradas. O zip é lido em memória, sem extrair para disco (sem zip-slip). Rejeitar YAML com âncoras/aliases excessivos (limite de tamanho).
- Preview markdown sem HTML cru; links com `rel="noopener noreferrer"`.
- Git sync (V2): token do GitHub cifrado com o `ApiKeyEncryptionService` (Data Protection, já existente) ou variável de ambiente `DIAX_`; nunca em texto puro no banco nem no frontend.
- MCP: reaproveita as credenciais de serviço já usadas pelo `diax-mcp` (env), sem secrets novos.
- Prompts podem conter dados sensíveis: export público só na V3, com aviso.

## 7. Riscos de integração

| # | Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|---|
| R1 | Migração rodando no startup de produção (timeout de 45 s) | Média | Alto | Migração só cria tabelas/índices novos, sem backfill pesado; o seed é feito por endpoint de import, não na migração |
| R2 | Regressão no Gerador ao extrair `ITextLlmClient` (V2) | Média | Alto | Testes de caracterização antes; PR separado; o MVP não toca no service |
| R3 | `user_prompts`/Snippets alterados por engano | Baixa | Alto | ADR-01; checklist de PR: diff sem arquivos desses módulos, exceto o botão "Salvar no Studio" |
| R4 | Deep link quebrado no host estático | Alta se usar `[id]` | Médio | ADR-03 (query string) |
| R5 | Lista de prompts MCP desatualizada no Claude Code | Alta | Baixo | Tools sempre disponíveis; documentar `/mcp` reconnect |
| R6 | Bundle do editor pesando o app | Média | Médio | `next/dynamic` + medição no build (`next build` output) |
| R7 | Conflito de edição web × MCP | Baixa | Médio | ADR-12 (409 + diff) |
| R8 | SQL compartilhado sem FTS / `LIKE` lento | Baixa | Baixo | ADR-06 + gatilho de revisão |
| R9 | `DESIGN.md` diverge do código | Já ocorre | Baixo | ADR-09 + tarefa de atualização |
| R10 | Outras sessões/branches mexendo em `AppShell.tsx` | Média | Baixo | Mudança de 1 linha no nav; rebase antes do PR |
| R11 | Limite de 256 chars nas strings (convenção do DbContext) cortando conteúdo | Alta se esquecer | Alto | Configuração explícita `nvarchar(max)` em `Content`/`VariablesJson`, com teste de integração com 20 KB |

## 8. Observabilidade
- Logs estruturados (Serilog já existente) para `prompt.created/versioned/used/imported`, com `source` (web|mcp|import|generator).
- `UseCount` + `LastUsedAt` no `Prompt` (incrementados por `POST /use`) alimentam a ordenação "mais usados" e o analytics da V3.
