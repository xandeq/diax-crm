# Prompt Studio — Modelo de dados

Convenções do `DiaxDbContext` aplicadas automaticamente: colunas em `snake_case`, `DateTime` em UTC, strings 256 por padrão (**sobrescrever onde indicado**), multi-tenant via `IUserOwnedEntity`.

## 1. Diagrama

```
prompt_collections 1───* prompts 1───* prompt_versions
                              │ 1
                              ├───* prompt_tags
                              │
                    (V2)      ├───* prompt_test_cases
                              └───* prompt_runs *───1 prompt_versions
                                        │ 1
                                        └───* prompt_evaluations *───0..1 prompt_test_cases
```

## 2. Tabelas do MVP

### `prompts`
| Coluna | Tipo | Regra |
|---|---|---|
| `id` | uniqueidentifier PK | |
| `user_id` | uniqueidentifier | FK `users`, filtro global |
| `title` | nvarchar(160) | obrigatório |
| `slug` | nvarchar(80) | `[a-z0-9-]`, único por usuário; gerado do título e editável; vira nome no MCP |
| `description` | nvarchar(500) null | |
| `collection_id` | uniqueidentifier null | FK `prompt_collections` ON DELETE SET NULL |
| `kind` | tinyint | 0=Prompt, 1=Template, 2=SystemPrompt |
| `framework` | nvarchar(40) null | chave de `promptTypeOptions` (`risen`, `costar`…) |
| `target_model` | nvarchar(80) null | ex.: `claude-sonnet-5` (informativo) |
| `current_version_number` | int | ≥ 1 |
| `preview` | nvarchar(280) | primeiros 280 chars da versão atual (denormalizado para a lista) |
| `variable_names` | nvarchar(500) null | CSV das variáveis da versão atual (lista e MCP sem join) |
| `is_favorite` | bit | default 0 |
| `is_archived` | bit | default 0 |
| `expose_to_mcp` | bit | default 0 |
| `source` | tinyint | 0=Manual, 1=Generator, 2=Snippet, 3=Import, 4=Mcp, 5=Git |
| `source_ref_id` | uniqueidentifier null | id do `user_prompts`/`snippets` de origem (idempotência da migração) |
| `use_count` | int | default 0 |
| `last_used_at` | datetime2 null | |
| `created_at` / `updated_at` | datetime2 | |
| `row_version` | rowversion | concorrência otimista (metadados) |

Índices: `UX(user_id, slug)`, `IX(user_id, is_archived, updated_at DESC)`, `IX(user_id, is_favorite)`, `IX(user_id, collection_id)`, `UX(user_id, source, source_ref_id) WHERE source_ref_id IS NOT NULL`.

### `prompt_versions`
| Coluna | Tipo | Regra |
|---|---|---|
| `id` | uniqueidentifier PK | |
| `prompt_id` | uniqueidentifier | FK ON DELETE CASCADE |
| `number` | int | 1..n, sequencial por prompt |
| `content` | **nvarchar(max)** | obrigatório, ≤ 100 KB (validação) |
| `variables_json` | **nvarchar(max)** null | definições tipadas (ADR-04) |
| `change_note` | nvarchar(300) null | |
| `content_hash` | char(64) | SHA-256 de `content + variables_json` |
| `source` | tinyint | mesmo enum de `prompts.source` (quem gerou essa versão) |
| `framework_applied` | nvarchar(40) null | quando a versão veio de "Reescrever com framework" |
| `created_at` | datetime2 | |

Índices: `UX(prompt_id, number)`. Imutável: sem update, sem soft delete; só sai pelo cascade do prompt.

### `prompt_tags`
| Coluna | Tipo |
|---|---|
| `prompt_id` | uniqueidentifier FK CASCADE |
| `name` | nvarchar(40), normalizado |

PK `(prompt_id, name)`, `IX(name)`. O isolamento por usuário vem via `prompts` (o filtro global se aplica ao navegar a partir de `Prompt`; consultas de tags sempre fazem join com `prompts`).

### `prompt_collections`
| Coluna | Tipo | Regra |
|---|---|---|
| `id` | uniqueidentifier PK | |
| `user_id` | uniqueidentifier | filtro global |
| `name` | nvarchar(60) | único por usuário |
| `icon` | nvarchar(40) null | nome do ícone Lucide |
| `color` | nvarchar(16) null | token (`green`, `amber`…) |
| `sort_order` | int | |
| `created_at` | datetime2 | |

## 3. Tabelas da V2

### `prompt_runs`
`id`, `user_id`, `prompt_id`, `version_number`, `provider` nvarchar(40), `model` nvarchar(80), `variables_json` nvarchar(max), `rendered_prompt` nvarchar(max), `output` nvarchar(max), `status` tinyint (0=ok, 1=erro), `error` nvarchar(1000) null, `latency_ms` int, `input_tokens` int null, `output_tokens` int null, `fallback_used` bit, `rating` tinyint null (1–5), `notes` nvarchar(1000) null, `test_case_id` uniqueidentifier null, `batch_id` uniqueidentifier null (agrupa uma comparação), `created_at`.
Índices: `IX(prompt_id, created_at DESC)`, `IX(batch_id)`. Retenção: as últimas 200 execuções por prompt (job de limpeza).

### `prompt_test_cases`
`id`, `prompt_id`, `name` nvarchar(120), `variables_json` nvarchar(max), `assertions_json` nvarchar(max) (`[{type: contains|not_contains|regex|json_valid|max_chars, value}]`), `created_at`.

### `prompt_evaluations`
`id`, `run_id` FK CASCADE, `test_case_id` null, `kind` tinyint (0=manual, 1=assertion, 2=llm_judge), `score` decimal(5,2) (0–100), `passed` bit null, `details_json` nvarchar(max) null, `created_at`.

## 4. Regras de domínio (invariantes)

1. `Prompt.Create(...)` sempre cria a v1 junto (transação única).
2. `Prompt.AddVersion(content, vars, note, source, baseVersion)`:
   - `baseVersion != CurrentVersionNumber` → erro `Conflict`.
   - hash igual ao da atual → retorna a atual (no-op, sem nova versão).
   - senão cria `number = current + 1`, atualiza `current_version_number`, `preview`, `variable_names` e `updated_at`.
3. `Restore(n)` = `AddVersion(content(n), vars(n), "Restaurado da v{n}", ...)`.
4. Slug: único por usuário; em colisão, sufixo `-2`, `-3`…; renomear o slug não mexe nas versões.
5. Tags: no máximo 20 por prompt; normalização igual no backend e no frontend.
6. Excluir prompt = hard delete com cascade (pede confirmação na UI). Arquivar é o caminho padrão.
7. Excluir coleção não exclui prompts (`SET NULL`).

## 5. Migração EF (aditiva)

`AddPromptLibrary` cria as 4 tabelas do MVP e os índices. **Não** altera `user_prompts` nem `snippets`. Sem dados semeados na migração (R1). A V2 vem numa migração separada, `AddPromptRunsAndEvals`.
