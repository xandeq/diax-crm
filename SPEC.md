# SPEC — Anexos de Arquivo em Snippets

> Spec-driven development para DIAX CRM. Escrito antes de codificar. Confirmar antes de implementar.

## 1. Objetivo

Estender o módulo **Snippets** (hoje 100% texto) para permitir **anexar e baixar arquivos**, sem quebrar o fluxo atual de salvar apenas texto/notas.

**Usuário-alvo:** Alexandre (single-user CRM). Usa snippets para guardar trechos de texto, prompts, SQL, notas — e agora também arquivos avulsos (zip, PDF, imagem, docs, qualquer coisa) que ele queira guardar e baixar depois, opcionalmente compartilhar via link público.

**Comportamento central:**
- Um snippet pode ter: **só texto**, **só arquivo(s)**, ou **ambos**.
- Texto deixa de ser obrigatório (hoje é). Passa a valer a regra: **um snippet precisa ter texto OU pelo menos um arquivo**.
- Um snippet aceita **vários arquivos** (lista de anexos).
- O dono autenticado sempre pode baixar. Se o snippet for **público**, qualquer pessoa com o link também pode baixar os anexos.

## 2. Decisões (confirmadas com o usuário)

| Decisão | Escolha |
|---|---|
| Arquivos por snippet | **Vários** (tabela separada `snippet_attachments`) |
| Texto | **Opcional** (exigir texto OU ≥1 arquivo) |
| Tipos de arquivo | **Qualquer tipo** |
| Tamanho máx. por arquivo | **20 MB** |
| Download público | **Sim**, se o snippet for público |

## 3. Escopo

### Inclui
- Nova entidade `SnippetAttachment` (1 snippet → N anexos).
- Endpoints de upload, download, listagem e remoção de anexos.
- Armazenamento em disco no padrão existente (`App_Data/...` por usuário), igual a TaxDocuments.
- UI na página de gerenciamento (`utilities/snippets`) para anexar arquivos ao criar um snippet, listar anexos e baixá-los.
- Download de anexos no viewer público (`/snippet`) quando o snippet for público.
- Migração de schema (aplicada em Produção via `update-db.ps1`).

### NÃO inclui
- Preview/renderização inline de arquivos (só download).
- Edição de snippet existente para adicionar anexos depois (criação carrega os arquivos junto; ver §6 para a abordagem escolhida).
- Versionamento de arquivos, antivírus, deduplicação.
- Alterar o modelo de expiração ou visibilidade.

## 4. Modelo de dados

### Entidade nova: `SnippetAttachment` (Diax.Domain/Snippets)
```
Id                Guid (PK)
SnippetId         Guid (FK → snippets.Id, cascade delete)
OriginalFileName  string  (nome real, exibido/baixado)
StoredFileName    string  (GUID + extensão, nome no disco)
ContentType       string
SizeBytes         long
CreatedAt         DateTime (UTC)
```
- Configuração EF em `Diax.Infrastructure/Data/Configurations/SnippetAttachmentConfiguration.cs`.
- Índice em `SnippetId`.
- Relação: `Snippet` ganha `ICollection<SnippetAttachment> Attachments` (navegação). Cascade delete: apagar snippet apaga registros de anexo (e os arquivos em disco — feito no controller/service, como em TaxDocuments).

### Mudança em `Snippet`
- `Content` deixa de ser obrigatório no domínio. Validação de "texto OU arquivo" fica no `SnippetService.CreateAsync` (precisa saber se há arquivos).
- `SetContent` aceita vazio; mantém sanitização quando houver valor.

### Armazenamento em disco
- Caminho: `{ContentRootPath}/App_Data/snippets/{userId}/{storedFileName}` — espelha `TaxDocumentsController.GetStorageDir`.

## 5. API (Diax.Api/Controllers/V1/SnippetsController.cs)

Mantém os endpoints atuais. Adiciona:

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `POST` | `/snippets` | autenticado | **Alterado**: aceita `multipart/form-data` com campos do snippet + 0..N arquivos (`files`). Valida texto-ou-arquivo, tamanho (20 MB cada), salva snippet + anexos. |
| `GET` | `/snippets/{id}/attachments/{attachmentId}/download` | autenticado | Baixa anexo (dono ou snippet público do próprio user). Retorna `File(stream, contentType, originalFileName)`. |
| `DELETE` | `/snippets/{id}/attachments/{attachmentId}` | autenticado | Remove um anexo (registro + arquivo em disco). |
| `GET` | `/snippets/public/{id}/attachments/{attachmentId}/download` | **AllowAnonymous** | Baixa anexo de snippet público (valida `IsPublic` e não-expirado). |

- `GET /snippets`, `GET /snippets/{id}`, `GET /snippets/public/{id}` passam a incluir a lista de anexos no DTO (id, nome, tipo, tamanho) — sem o conteúdo binário.
- Limites: `[Consumes("multipart/form-data")]`, `[RequestSizeLimit]` adequado a múltiplos arquivos (ex.: ~110 MB para até ~5 arquivos; total enforçado por arquivo a 20 MB e validação de contagem no service).
- Erros mapeados via padrão atual (`BadRequest`/`NotFound`/`Forbid`).

### DTOs (Diax.Application/Snippets/Dtos)
- `CreateSnippetRequestDto`: vira form-bindable (campos atuais; `Content` opcional). Arquivos entram como `IFormFile[]` no parâmetro do controller, não no DTO.
- `SnippetResponseDto`: adiciona `List<SnippetAttachmentDto> Attachments`.
- Novo `SnippetAttachmentDto`: `Id, OriginalFileName, ContentType, SizeBytes, CreatedAt`.

### Service (`SnippetService` / `ISnippetService`)
- `CreateAsync` recebe os metadados dos arquivos já salvos (originalName, storedName, contentType, size) — controller grava o arquivo em disco, service persiste entidade + anexos (espelha o fluxo de TaxDocuments: controller faz I/O, service faz DB; em falha de DB, controller limpa arquivos).
- Validação nova: se `Content` vazio **e** zero arquivos → `ArgumentException("Informe um texto ou ao menos um arquivo.")`.
- Métodos novos: `GetAttachmentDownloadInfoAsync`, `DeleteAttachmentAsync` (validam ownership / público + expiração).

## 6. Frontend

### Service (`crm-web/src/services/snippetService.ts`)
- `createSnippet` passa a aceitar `File[]` e enviar `FormData` (não JSON) quando houver arquivos — manter compatível com `apiFetch` (suportar body `FormData` sem forçar `Content-Type`).
- `SnippetResponse` ganha `attachments: SnippetAttachment[]`.
- Novos métodos: `downloadAttachment(snippetId, attachmentId)`, `downloadPublicAttachment(snippetId, attachmentId)`, `deleteAttachment(snippetId, attachmentId)`. Download faz fetch autenticado → `blob` → `URL.createObjectURL` → clique programático.

### Página de gerenciamento (`crm-web/src/app/utilities/snippets/page.tsx`)
- Form: adicionar `<input type="file" multiple>` (estilizado com shadcn/Button) + lista dos arquivos selecionados (nome, tamanho, remover antes de enviar).
- Validação client: texto OU ≥1 arquivo; cada arquivo ≤ 20 MB.
- Texto deixa de ser obrigatório no `handleSave` (ajustar mensagem).
- No card de cada snippet: listar anexos com botão **Baixar** (ícone Download) e, opcionalmente, remover anexo.

### Viewer público (`crm-web/src/components/snippets/SnippetPublicClient.tsx`)
- Se o snippet público tiver anexos, exibir lista com botão Baixar (usa rota pública).

## 7. Comandos

```bash
# Backend
cd api-core && dotnet build
cd api-core && dotnet test --filter "FullyQualifiedName~SnippetServiceTests"

# Migração (gera código localmente)
cd api-core; .\scripts\add-migration.ps1 -Name "AddSnippetAttachments"
# Aplica em PRODUÇÃO (padrão obrigatório do projeto)
cd api-core; .\scripts\update-db.ps1

# Frontend
cd crm-web && npm run dev
cd crm-web && npm run build
```

## 8. Estilo de código (seguir o repo)

- Backend: Clean Architecture. Domain sem deps externas; lógica no Application; I/O de arquivo no controller (igual TaxDocuments); `Result<T>` / `HandleResult` onde já se usa, ou try/catch como no `SnippetsController` atual (manter consistência com o controller existente).
- Convenções DB automáticas do `DiaxDbContext` (snake_case, UTC, max length 256, etc.).
- Frontend: chamadas só em `src/services/`; componentes shadcn de `src/components/ui/`; nada de `fetch` direto em componente.
- Mensagens de validação em **português** (consistente com o módulo).

## 9. Estratégia de testes

- **Unit (xUnit/Moq/FluentAssertions)** em `Diax.UnitTests`:
  - `CreateAsync` rejeita quando sem texto e sem arquivos.
  - `CreateAsync` aceita só-texto, só-arquivo, e ambos.
  - `DeleteAttachmentAsync` bloqueia anexo de outro usuário.
  - Download público só funciona com `IsPublic = true` e não-expirado.
- **Manual:** subir arquivo >20 MB (rejeita), tipo arbitrário (.zip) sobe e baixa íntegro, snippet só-arquivo aparece na lista, link público baixa anexo, deletar snippet remove arquivos do disco.

## 10. Fronteiras (boundaries)

**Sempre:**
- Migração aplicada via `update-db.ps1` contra Produção (regra crítica do projeto). Nunca LocalDB.
- Limpar arquivo do disco se a gravação no DB falhar; remover arquivos ao deletar snippet/anexo.
- Validar ownership em toda operação autenticada; validar `IsPublic` + expiração em rota pública.

**Perguntar antes:**
- Mudar o modelo de visibilidade/expiração.
- Aumentar o limite de 20 MB ou nº de arquivos por snippet.
- Adicionar storage externo (S3/FTP) em vez de `App_Data`.

**Nunca:**
- Servir anexo de snippet privado sem autenticação.
- Confiar no `Content-Type` enviado pelo cliente para decisões de segurança (apenas para o header de download).
- Hardcode de secrets/paths fora do padrão do projeto.

---

### Pontos de atenção para deploy (App_Data no SmarterASP)
- `App_Data/snippets/` precisa de permissão de escrita no host (mesmo requisito do módulo TaxDocuments — já funciona, então OK).
- Arquivos ficam fora do banco; backup do DB **não** cobre os anexos. Documentar.
