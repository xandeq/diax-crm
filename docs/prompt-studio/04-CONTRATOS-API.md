# Prompt Studio — Contratos de API e MCP

Base: `/api/v1`, JWT Bearer (o `apiFetch` anexa automaticamente). Os erros seguem o `HandleResult` do `BaseApiController`: `Validation`→400, `NotFound`→404, `Conflict`→409. Corpo de erro: `{ "code": "Prompt.Conflict", "message": "...", "details": {...} }`.

## 1. Tipos compartilhados

```ts
type PromptKind = 'prompt' | 'template' | 'system';
type PromptSource = 'manual' | 'generator' | 'snippet' | 'import' | 'mcp' | 'git';

interface PromptVariable {
  name: string;               // ^[a-zA-Z_][\w.-]{0,63}$
  label?: string;
  type: 'text' | 'textarea' | 'select' | 'number';
  default?: string;
  required: boolean;          // default true
  options?: string[];         // type=select
}

interface PromptListItem {
  id: string; slug: string; title: string; description?: string;
  kind: PromptKind; framework?: string; collectionId?: string;
  tags: string[]; preview: string; variableNames: string[];
  isFavorite: boolean; isArchived: boolean; exposeToMcp: boolean;
  currentVersionNumber: number; useCount: number;
  lastUsedAt?: string; updatedAt: string;
}

interface PromptVersion {
  number: number; content: string; variables: PromptVariable[];
  changeNote?: string; source: PromptSource; frameworkApplied?: string;
  createdAt: string;
}

interface PromptDetail extends PromptListItem {
  targetModel?: string; source: PromptSource; // (rowVersion não implementado — ver nota em §2)
  current: PromptVersion;
  createdAt: string;
}
```

## 2. Prompts (MVP)

> **Implementado (`PromptsController`)** — diferenças do contrato abaixo:
> - `GET /prompts` aceita `q, tag, collectionId, favorite, kind, archived, exposeToMcp, sort (updated|used|title|created), limit (≤2000)` e devolve **array** (sem `PagedResult`/`ETag`).
> - Resposta de detalhe = `{ prompt: PromptListItem, targetModel, current: PromptVersion }`.
> - `PATCH` sem `If-Match`; `clearCollection: true` remove a coleção.
> - `POST /use` sem corpo. `GET /prompts/tags` e `POST /tags/rename` ok.
> - **Ainda não implementados:** `/rewrite`, `/from-generator/bulk`, export/import `.json`.
> - `POST /prompts/export` (body `{ ids, format, flavor }`) é o usado pela UI: muitos GUIDs no GET estouram o `maxQueryString` do IIS. Máx. 2.000 prompts por export; leitura em lote (`GetManyAsync`).
> - Falha de banco num item (import, create, versão) limpa o ChangeTracker para não contaminar as operações seguintes. Corrida em índice único: mesma origem → devolve o existente; slug → 2ª tentativa; versão → 409.
>
> **Import/export implementado (`PromptTransferService`):**
> - `GET /prompts/export?format=md|zip&flavor=diax|claude-command&ids=<guid>&ids=…` → `format=md` com 1 id devolve `<slug>.md`; o resto devolve `.zip` (pastas por coleção no sabor DIAX; arquivos na raiz no sabor Claude Code). Sem `ids` = todos os não arquivados. CORS expõe `Content-Disposition`.
> - `POST /prompts/import` (multipart `files[]`, `dryRun` padrão `true`, `strategy=new-version|skip|duplicate`) aceita `.md/.markdown/.txt/.zip`. Match por `diax.id` (se for do usuário) e depois por slug. Conteúdo e variáveis iguais = `unchanged`. A estratégia `new-version` **só versiona o conteúdo** (não mexe em tags/coleção do existente). Coleções são criadas pelo nome sob demanda. Limites: 50 arquivos por request, 5 MB por arquivo/zip, 512 KB por `.md`, 500 entradas e 20 MB descompactados por zip, UTF-8 obrigatório. Ações por item: `create|newVersion|unchanged|skip|error`.
> - `$ARGUMENTS/$N` só é convertido em `{{var}}` em commands do Claude Code (sem bloco `diax` ou `diax.flavor: claude-command`). Nomes vêm das variáveis declaradas, depois do `argument-hint`, depois `arguments`/`argN`. Em arquivo DIAX, `$5,00` é texto literal.

| Método | Rota | Descrição |
|---|---|---|
| GET | `/prompts` | Lista paginada / índice |
| GET | `/prompts/{id}` | Detalhe + versão atual |
| GET | `/prompts/by-slug/{slug}` | Detalhe por slug (MCP) |
| POST | `/prompts` | Cria (gera a v1) |
| PATCH | `/prompts/{id}` | Metadados (não cria versão) |
| DELETE | `/prompts/{id}` | Exclusão definitiva (cascade) |
| POST | `/prompts/{id}/duplicate` | Cópia com `title + " (cópia)"`, v1 = atual |
| POST | `/prompts/{id}/use` | Registra uso (`useCount++`), body `{ channel: 'web'|'mcp'|'palette' }` |
| POST | `/prompts/{id}/render` | Render das variáveis |
| GET | `/prompts/{id}/versions` | Lista de versões (sem `content`, com `changeNote`/data) |
| GET | `/prompts/{id}/versions/{n}` | Versão completa |
| POST | `/prompts/{id}/versions` | Nova versão |
| POST | `/prompts/{id}/versions/{n}/restore` | Cria versão nova = v{n} |
| POST | `/prompts/{id}/rewrite` | "Reescrever com framework" → retorna proposta, **não salva** |
| GET | `/prompts/tags` | `[{ name, count }]` |
| POST | `/prompts/tags/rename` | `{ from, to }` → renomeia/mescla em lote |

### GET `/prompts`
Query: `q`, `tag` (repetível, E lógico), `collectionId`, `favorite`, `kind`, `archived` (default false), `sort` = `updated|used|title|created` (default `updated`), `page` (1), `pageSize` (50, máximo 200), `fields=index`.
- `fields=index` retorna **todos** os não arquivados, sem paginação, só com os campos de `PromptListItem` (para o filtro local e a palette). Cache com `ETag` (hash de `max(updated_at)` + count) → `304`.
- Resposta paginada: `PagedResult<PromptListItem>` (`items`, `page`, `pageSize`, `totalCount`) do `Diax.Shared`.

### POST `/prompts`
```json
{
  "title": "Revisor de código sênior",
  "slug": "revisor-codigo",            // opcional
  "description": "Revisão P0/P1/P2 com foco em bugs",
  "content": "Atue como engenheiro sênior...\n{{codigo}}",
  "variables": [{ "name": "codigo", "type": "textarea", "required": true }],
  "tags": ["dev", "review"],
  "collectionId": null,
  "kind": "template",
  "framework": "risen",
  "targetModel": "claude-sonnet-5",
  "exposeToMcp": true,
  "changeNote": "Versão inicial",
  "source": "manual",                  // servidor aceita manual|mcp|generator|snippet|import
  "sourceRefId": null
}
```
→ `201 PromptDetail`. Validação: `title` 1–160, `content` 1–100 KB, ≤ 20 tags, variáveis declaradas com nome válido. Variáveis usadas no conteúdo e não declaradas são inferidas como `text` obrigatória. Declaradas e não usadas → aviso (`warnings[]`), não erro.

### PATCH `/prompts/{id}`
> ⚠️ Proposta original abaixo; **implementado sem `If-Match`/412** (ver nota no topo de §2).

Header `If-Match: <rowVersion>`. Body parcial: `title, slug, description, tags, collectionId, kind, framework, targetModel, isFavorite, isArchived, exposeToMcp`. `412` se o rowVersion divergir. Favoritar/arquivar não exigem `If-Match` (operações idempotentes, last-write-wins aceitável).

### POST `/prompts/{id}/versions`
```json
{ "content": "...", "variables": [...], "changeNote": "Mais exemplos", "baseVersionNumber": 6, "source": "manual" }
```
- `201 PromptVersion` (nova v7).
- `200 PromptVersion` com header `X-Prompt-NoChange: true` se o hash for igual ao da atual.
- `409` `{ code: "Prompt.Conflict", details: { current: PromptVersion } }` se `baseVersionNumber` ≠ atual.

### POST `/prompts/{id}/render`
```json
{ "variables": { "codigo": "..." }, "versionNumber": null, "strict": false }
```
→ `200 { "rendered": "...", "missing": ["lead"], "unused": [] }`. Com `strict=true` e `missing` não vazio → `400`. Os defaults são aplicados antes de calcular `missing`.

### POST `/prompts/{id}/rewrite`
`{ "framework": "costar", "provider": "anthropic", "model": null }` → chama internamente o mesmo fluxo do `PromptGeneratorService.GenerateAsync` com o conteúdo atual como `rawPrompt`. Resposta: `{ "proposal": "...", "provider": "deepseek", "fallbackUsed": true }`. O usuário revisa o diff e salva como versão (`source=generator`, `frameworkApplied=costar`). Erros de provider repassados no formato `PromptErrorResponse` atual.

## 3. Coleções (MVP)

| Método | Rota | Body/obs. |
|---|---|---|
| GET | `/prompt-collections` | `[{ id, name, icon, color, sortOrder, promptCount }]` |
| POST | `/prompt-collections` | `{ name, icon?, color? }` → 201 |
| PATCH | `/prompt-collections/{id}` | `{ name?, icon?, color?, sortOrder? }` |
| PUT | `/prompt-collections/order` | `{ ids: [...] }` (drag para reordenar) |
| DELETE | `/prompt-collections/{id}` | Prompts ficam sem coleção |

## 4. Migração / origens (MVP)

| Método | Rota | Descrição |
|---|---|---|
| POST | `/prompts/from-generator/{userPromptId}` | Cria o prompt a partir de `user_prompts`: `content = generatedPrompt`, `framework = promptType`, `description = inputPreview`, `source = generator`. Idempotente via `(source, source_ref_id)` → 200 com o existente |
| POST | `/prompts/from-snippet/{snippetId}` | Só para `language ∈ {text, markdown}`. Anexos não migram (aviso). Idempotente |
| POST | `/prompts/from-generator/bulk` | `{ ids: [...] }` → `{ created, existing, failed[] }` |

## 5. Import / Export (MVP)

### GET `/prompts/export`
Query: `format = md|zip|json`, `ids` (opcional; vazio = todos), `includeHistory` (json apenas), `flavor = diax|claude-command`.
- `md`: um prompt (`ids` com 1 item) → `text/markdown`, `Content-Disposition: attachment; filename="<slug>.md"`.
- `zip`: `<colecao>/<slug>.md` + `manifest.json`.
- `json`: `{ schemaVersion: 1, exportedAt, prompts: [PromptDetail & { versions?: PromptVersion[] }] }`.
- `flavor=claude-command`: converte `{{var}}` em argumentos. Com 1 variável → `$ARGUMENTS`; com N variáveis → `$1..$N` na ordem de declaração + `argument-hint: [var1] [var2]`.

**Formato `.md` canônico (ADR-08):**
```markdown
---
description: Revisão P0/P1/P2 com foco em bugs
argument-hint: "[codigo]"
diax:
  title: Revisor de código sênior
  slug: revisor-codigo
  kind: template
  framework: risen
  collection: Dev
  tags: [dev, review]
  targetModel: claude-sonnet-5
  exposeToMcp: true
  variables:
    - { name: codigo, type: textarea, required: true }
  id: 3f2a9c1e-...
  version: 7
---
Atue como engenheiro sênior...
{{codigo}}
```
Arquivos sem o bloco `diax` (ex.: um command do Claude Code) são aceitos: `title` = nome do arquivo, `description` = `description`, `$ARGUMENTS` → `{{arguments}}`.

### POST `/prompts/import`
`multipart/form-data`: `files[]` (`.md`, `.zip`, `.json`), `dryRun` (default `true`), `strategy = skip|new-version|duplicate` (quando o slug ou `diax.id` já existe).
→ `200`:
```json
{
  "dryRun": true,
  "summary": { "create": 4, "newVersion": 1, "skip": 0, "errors": 1 },
  "items": [
    { "file": "dev/revisor-codigo.md", "action": "newVersion", "slug": "revisor-codigo", "warnings": [] },
    { "file": "x.md", "action": "error", "error": "Frontmatter YAML inválido (linha 3)" }
  ]
}
```
Limites: `.md` ≤ 512 KB, `.zip` ≤ 5 MB / 500 arquivos; `413` acima disso. Cada item é processado em transação própria; um erro não aborta o lote.

## 6. V2: Playground, testes, avaliações, Git

| Método | Rota | Descrição |
|---|---|---|
| POST | `/prompts/{id}/runs` | `{ versionNumber?, variables, provider, model?, temperature?, testCaseId? }` → executa via `ITextLlmClient` (fallback de providers) e grava `PromptRun` |
| POST | `/prompts/{id}/compare` | `{ variables, targets: [{ versionNumber, provider, model }] (2–4) }` → execuções em paralelo com o mesmo `batchId` |
| GET | `/prompts/{id}/runs?limit=` | Histórico de execuções |
| PATCH | `/prompt-runs/{runId}` | `{ rating?, notes? }` |
| GET/POST/PATCH/DELETE | `/prompts/{id}/test-cases[/{tcId}]` | CRUD de casos de teste |
| POST | `/prompts/{id}/test-cases/run` | `{ versionNumber?, provider, model? }` → roda todos, avalia asserções → `{ passed, failed, results[] }` |
| GET | `/prompts/{id}/stats` | Média de nota e taxa de aprovação por versão |
| GET/PUT | `/prompt-studio/git-config` | `{ repo: "owner/name", branch, path, tokenSet: bool }` (token só escrito, nunca lido) |
| POST | `/prompt-studio/git-sync` | Push one-way: gera os `.md` e faz commit via GitHub Contents/Trees API → `{ commitSha, changed, unchanged }` |

## 7. MCP (`diax-mcp`)

### Capability
```ts
new Server({ name: "diax-crm", version: "1.1.0" },
           { capabilities: { tools: {}, prompts: {} } });
```

### Tools novas (MVP)

| Tool | Input | Output |
|---|---|---|
| `search_prompts` | `{ query?: string, tag?: string, collection?: string, favorite?: boolean, limit?: number (≤25) }` | Lista compacta: `slug — título [tags] (vN, usado X×)` |
| `get_prompt` | `{ slug_or_id: string, version?: number }` | Markdown canônico (frontmatter + corpo) |
| `render_prompt` | `{ slug_or_id: string, variables?: Record<string,string> }` | Texto renderizado; se faltar variável, lista as faltantes com label/default (sem erro). Registra `use` com `channel=mcp` |
| `save_prompt` | `{ title: string, content: string, tags?: string[], collection?: string, description?: string, slug_or_id?: string, change_note?: string, expose_to_mcp?: boolean }` | Sem `slug_or_id` → cria; com → nova versão usando `baseVersionNumber` = atual buscada na hora (conflito improvável; em caso de 409, devolve a mensagem para o usuário decidir). `source=mcp` |

### Prompts capability
- `prompts/list` → **`GET /prompts/mcp-catalog`** (implementado: expostos, máx. 100, por uso, com as variáveis da versão atual; argumento é `required` só se a variável for obrigatória **e** sem default). `prompts/get` recusa prompt não exposto:
  ```json
  { "name": "revisor-codigo", "title": "Revisor de código sênior",
    "description": "Revisão P0/P1/P2 com foco em bugs",
    "arguments": [{ "name": "codigo", "description": "Código a revisar", "required": true }] }
  ```
- `prompts/get` `{ name, arguments }` → `POST /prompts/by-slug/{name}/render` → `{ messages: [{ role: "user", content: { type: "text", text: rendered } }] }` + `use` (`channel=mcp`).
- No Claude Code aparece como **`/mcp__diax-crm__revisor-codigo`** (o prefixo segue o nome do servidor registrado no `claude mcp add`).
- Cache da lista no processo do MCP: 60 s.

### Rota auxiliar usada pelo MCP
`POST /prompts/by-slug/{slug}/render`: mesmo contrato do `/render`, endereçado por slug.
