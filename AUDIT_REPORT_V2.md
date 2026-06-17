# AUDIT_REPORT_V2 — ErrorLogs Feature
**Data:** 2026-06-05  
**Auditor:** Claude Code (linha por linha)  
**Escopo:** 12 arquivos criados + 4 modificados no commit `190ffdf`  
**Status:** ⛔ BLOQUEADO — 4 bugs críticos impedem avanço para UI

---

## ⛔ PROBLEMAS CRÍTICOS — Parar antes de continuar

---

### C-01 — Resolve NUNCA persiste no banco
**Severidade:** Crítica | **Probabilidade:** 100% | **Impacto:** Funcionalidade completamente quebrada

**Arquivo:** `ErrorLogRepository.cs:15-16` + `ErrorLogService.cs:121-131`

```csharp
// Repository — GetByIdAsync usa AsNoTracking
public async Task<ErrorLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
    => await _db.ErrorLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

// Service — chama GetByIdAsync (sem tracking), modifica, salva
public async Task<Result<ErrorLogResponse>> ResolveAsync(Guid id, string? note, CancellationToken ct = default)
{
    var log = await _repo.GetByIdAsync(id, ct);  // ← entidade NÃO rastreada
    log.Resolve(note);                             // ← modificação invisível ao EF
    await _repo.SaveChangesAsync(ct);             // ← salva NADA (change tracker vazio)
    return Result.Success(ErrorLogResponse.FromEntity(log)); // ← retorna 200 OK mentiroso
}
```

**O que acontece:** PUT `/error-logs/{id}/resolve` retorna HTTP 200 com o log aparentemente resolvido, mas o banco não é atualizado. `IsResolved` permanece `false` para sempre.

**Correção:** `GetByIdAsync` precisa de duas sobrecargas, ou o repositório precisa de um método rastreado para ResolveAsync:
```csharp
// Opção simples: remover AsNoTracking de GetByIdAsync e criar GetByIdNoTrackingAsync para leitura pura
public async Task<ErrorLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
    => await _db.ErrorLogs.FirstOrDefaultAsync(x => x.Id == id, ct); // sem AsNoTracking
```

---

### C-02 — Race Condition no Dedupe (duplicatas garantidas sob carga)
**Severidade:** Crítica | **Probabilidade:** Alta sob concorrência | **Impacto:** Contagem de ocorrências incorreta, linhas duplicadas

**Arquivo:** `ErrorLogService.cs:174-187`

```csharp
// Check-then-act sem atomicidade
var existing = await _repo.GetOpenByFingerprintAsync(fingerprint, req.AppName, ct); // ← lê
if (existing is not null)
{
    existing.RecordOccurrence(occurredAt); // ← incrementa em memória
    await _repo.SaveChangesAsync(ct);      // ← salva
    return ...
}
// Duas requests com mesmo fingerprint simultâneas:
// A e B leem existing = null ao mesmo tempo
// A cria novo log → ID=1 para fingerprint X
// B cria novo log → ID=2 para fingerprint X (DUPLICATA)
await _repo.AddAsync(log, ct);
await _repo.SaveChangesAsync(ct);
```

**Cenário real:** InvestIQ entra em loop de erro. Batch de 10 logs chega ao mesmo tempo. Em vez de 1 log com count=10, criam-se 10 logs com count=1.

**Correção:** Usar `ExecuteUpdateAsync` atômico ou adicionar unique index em `(fingerprint, app_name)` WHERE `is_resolved = 0` para rejeitar duplicatas no banco e tratar o conflito:
```csharp
// No banco: UNIQUE INDEX IX_error_logs_fingerprint_unique ON error_logs(fingerprint, app_name)
//           WHERE is_resolved = 0 AND fingerprint IS NOT NULL

// No serviço: tratar SqlException de unique constraint como sinal de atualizar existing
```

---

### C-03 — TruncateUtf8 corrompe strings com caracteres multi-byte
**Severidade:** Crítica | **Probabilidade:** Média (qualquer stack trace com caractere não-ASCII) | **Impacto:** Dados corrompidos no banco, possível `[REDACTED]` aplicado incorretamente após corrupção

**Arquivo:** `ErrorLogService.cs:260-266`

```csharp
private static string TruncateUtf8(string? input, int maxBytes)
{
    if (string.IsNullOrEmpty(input)) return string.Empty;
    var bytes = Encoding.UTF8.GetBytes(input);
    if (bytes.Length <= maxBytes) return input;
    return Encoding.UTF8.GetString(bytes, 0, maxBytes) + "...[truncated]";
    // ↑ PROBLEMA: corta no meio de sequência multi-byte
    // Emoji (4 bytes), acentos em Python (ã, é, ç = 2 bytes cada) → U+FFFD no banco
}
```

**Exemplo concreto:** Stack trace de app Python com `ão` no path ou no nome do arquivo. A truncagem produz `\xc3` isolado (metade do 'ã' em UTF-8), que o .NET converte em `U+FFFD` (caractere de substituição). O banco armazena dados corrompidos.

**Correção:**
```csharp
private static string TruncateUtf8(string? input, int maxBytes)
{
    if (string.IsNullOrEmpty(input)) return string.Empty;
    var bytes = Encoding.UTF8.GetBytes(input);
    if (bytes.Length <= maxBytes) return input;
    // Recua até encontrar início de sequência UTF-8 válida
    var safeLength = maxBytes;
    while (safeLength > 0 && (bytes[safeLength] & 0xC0) == 0x80)
        safeLength--;
    return Encoding.UTF8.GetString(bytes, 0, safeLength) + "...[truncated]";
}
```

---

### C-04 — Paginação Cursor completamente quebrada
**Severidade:** Crítica | **Probabilidade:** 100% | **Impacto:** UI de paginação nunca funciona; page 2 = page 1

**Arquivo:** `ErrorLogRepository.cs:52-54`

```csharp
// Cursor-based: Id > cursor (cursor = last seen Id as string)
if (!string.IsNullOrWhiteSpace(filter.Cursor) && Guid.TryParse(filter.Cursor, out var cursorId))
    query = query.Where(x => x.Id != cursorId); // simplificado: offset by CreatedAt
// ↑ ISSO NÃO É CURSOR PAGINATION
// Apenas exclui 1 registro específico
// Página 2 retorna os mesmos 100 items da página 1 (menos esse 1 ID)
```

**O que a UI verá:** Scroll infinito nunca avança — sempre os mesmos logs de erro na tela.

**Correção:** Keyset pagination real:
```csharp
// Cursor = base64(occurredAt ISO + "|" + id)
if (!string.IsNullOrWhiteSpace(filter.Cursor))
{
    var (cursorDate, cursorId) = DecodeCursor(filter.Cursor);
    query = query.Where(x =>
        x.OccurredAt < cursorDate ||
        (x.OccurredAt == cursorDate && x.Id.CompareTo(cursorId) < 0));
}
```

---

## 🔴 PROBLEMAS ALTOS

---

### A-01 — Race Condition no Rate Limiter (bypass por concorrência)
**Severidade:** Alta | **Probabilidade:** Alta sob carga normal | **Impacto:** Rate limit ineficaz

**Arquivo:** `ErrorLogService.cs:232-246`

```csharp
var bucket = _cache.GetOrCreate(cacheKey, entry => { ... return new RateBucket(); })!;

if (bucket.Count + count > RateLimitPerMinute) return Failure(...);
bucket.Count += count;  // ← int simples, NÃO thread-safe
```

`RateBucket.Count` é um `int` sem `Interlocked`. Duas requests simultâneas leem `Count = 49`, ambas passam na validação, ambas incrementam. Resultado: 51+ requests por minuto passam com rate limit de 50.

**Correção:**
```csharp
private sealed class RateBucket
{
    private int _count;
    public int Count => _count;
    public int Increment(int by) => Interlocked.Add(ref _count, by);
}
// No CheckRateLimit:
var after = bucket.Increment(count);
if (after > RateLimitPerMinute) { bucket.Increment(-count); return Failure(...); }
```

---

### A-02 — `RecordUsage()` da ApiKey nunca salva no banco
**Severidade:** Alta | **Probabilidade:** 100% | **Impacto:** `LastUsedAt` sempre nulo/desatualizado na admin UI

**Arquivo:** `ErrorLogService.cs:228-229`

```csharp
apiKey.RecordUsage(); // ← modifica LastUsedAt em memória
return Result.Success(); // ← sem SaveChanges — modificação descartada
```

`RecordUsage()` chama `LastUsedAt = DateTime.UtcNow` na entidade, mas o EF nunca persiste porque não há `SaveChangesAsync`. O admin vê "Last used: never" para todas as keys de error log.

**Correção:** Adicionar `await _apiKeyRepo.SaveChangesAsync(ct)` ou aceitar que RecordUsage é best-effort e não chamar no hot path de ingestão (alternativa válida por performance).

---

### A-03 — Scope check vulnerável a substring bypass
**Severidade:** Alta | **Probabilidade:** Baixa (requer key criada maliciosamente) | **Impacto:** Key com escopo `error-logs.ingest-admin` passa como `error-logs.ingest`

**Arquivo:** `ErrorLogService.cs:225`

```csharp
if (!apiKey.Scope.Contains(RequiredScope, StringComparison.OrdinalIgnoreCase))
// RequiredScope = "error-logs.ingest"
// "error-logs.ingest-admin" → Contains("error-logs.ingest") == TRUE → bypass
```

**Correção:**
```csharp
var scopes = apiKey.Scope.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
if (!scopes.Any(s => s.Equals(RequiredScope, StringComparison.OrdinalIgnoreCase)))
    return Result.Failure(...);
```

---

### A-04 — PII scrubbing não cobre RequestPath, Source, ExceptionType, UserId
**Severidade:** Alta | **Probabilidade:** Média | **Impacto:** LGPD — dados pessoais vazam nos campos não scrubados

**Arquivo:** `ErrorLogService.cs:157-166` + `ErrorLogService.cs:190-203`

```csharp
var message     = ScrubPii(...);   // ✅ scrubbed
var stackTrace  = ScrubPii(...);   // ✅ scrubbed  
var additionalData = ScrubPii(...); // ✅ scrubbed

// ❌ Campos SEM scrubbing persistidos diretamente do request:
req.RequestPath     // URL pode conter CPF: /api/customers/096.613.297-10/profile
req.ExceptionType   // Raro mas possível em custom exceptions
req.Source          // Caminho de arquivo pode conter username do sistema
req.UserId          // Apps podem usar email como UserId
```

**Exemplo real:**  
InvestIQ: `GET /api/carteira/096.613.297-10/posicoes` → `RequestPath` persiste CPF.

**Correção:** Aplicar `ScrubPii()` em `requestPath`, `userId`, `exceptionType` antes de chamar `ErrorLog.Create()`.

---

### A-05 — OccurredAt sem validação — ataques de backdating e futuro
**Severidade:** Alta | **Probabilidade:** Baixa (requer key comprometida) | **Impacto:** Inserção de logs em datas arbitrárias, poluição de histórico, bypass de retenção

**Arquivo:** `ErrorLogService.cs:172`

```csharp
var occurredAt = req.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow;
// App maliciosa pode enviar: OccurredAt = "2000-01-01" (envelopa logs no histórico)
// Ou: OccurredAt = "2099-01-01" (nunca apagado pela retenção de 90 dias)
```

**Correção:**
```csharp
var occurredAt = req.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow;
var now = DateTime.UtcNow;
if (occurredAt > now.AddHours(1))    occurredAt = now; // futuro: clamp para agora
if (occurredAt < now.AddDays(-30))   occurredAt = now; // muito antigo: clamp para agora
```

---

### A-06 — AppName não vinculado à API Key (cross-app log injection)
**Severidade:** Alta | **Probabilidade:** Baixa | **Impacto:** App A pode postar logs como se fosse App B

**Arquivo:** `ErrorLogService.cs:157-208`

A API Key tem um nome (ex: "investiq-logger") mas não há validação de que `request.AppName == "investiq"`. Uma key comprometida pode injetar logs como:
```json
{ "appName": "vaganagringa", "message": "falsa mensagem de erro" }
```

**Correção:** Adicionar campo `AppSlug` na entidade `ApiKey` (ou usar `Name` como binding) e validar que `request.AppName` bate com o `ApiKey.Name/Scope` esperado. Alternativa: derivar o AppName da API Key automaticamente, ignorando o campo no request.

---

### A-07 — Hashing duplo da API Key por ingest
**Severidade:** Média-Alta | **Impacto:** Performance + indica lógica inconsistente

**Arquivo:** `ErrorLogService.cs:44-48`

```csharp
var authResult = await ValidateApiKeyAsync(apiKeyPlain, ct);  // ← hasheia internamente
// ...
var keyHash = ApiKey.HashKey(apiKeyPlain);  // ← hasheia de novo, desnecessariamente
var rateLimitResult = CheckRateLimit(keyHash);
```

`ValidateApiKeyAsync` já chama `ApiKey.HashKey` internamente. O resultado do hash deve ser retornado ou cacheado. Não é bug de segurança, mas indica falta de coordenação.

---

## 🟡 PROBLEMAS MÉDIOS

---

### M-01 — Level ausente no Fingerprint (Critical dedupado como Error)
**Arquivo:** `ErrorLog.cs:114-122`

```csharp
var raw = $"{appName}|{exceptionType ?? ""}|{source ?? ""}|{lineNumber?.ToString() ?? ""}";
// Level não incluído
```

Se o mesmo erro começa como `Error` (nível 1) e evolui para `Critical` (nível 2) em requests subsequentes, ambos são agrupados no mesmo fingerprint. O log original permanece como `Error` mesmo que todas as ocorrências recentes sejam `Critical`. O campo `Level` deve ser incluído no fingerprint ou o dedupe deve considerar escalada de nível.

---

### M-02 — Environment ausente no Fingerprint (staging mistura com prod)
**Arquivo:** `ErrorLog.cs:119`

Mesmo erro de staging e produção têm o mesmo fingerprint. A contagem de ocorrências de prod é inflada por erros de staging (ou vice-versa). Para o caso de uso "identificar erros em produção", isso cria ruído.

---

### M-03 — AdditionalData: double serialization para inputs string
**Arquivo:** `ErrorLogService.cs:164-166`

```csharp
var additionalData = req.AdditionalData is not null
    ? ScrubPii(TruncateUtf8(JsonSerializer.Serialize(req.AdditionalData), MaxPayloadBytes))
    : null;
```

`req.AdditionalData` é `object?`. Se o cliente enviar `"additionalData": "texto simples"`, o JSON deserializer produz uma `string`, e `JsonSerializer.Serialize("texto simples")` produz `"\"texto simples\""` (com aspas escapadas). O banco armazena a string duplamente serializada. O frontend precisaria deserializar duas vezes ou exibir com aspas extras.

**Correção:** Usar `JsonElement` no DTO e serializar apenas se necessário, ou armazenar como string diretamente se já vier como string.

---

### M-04 — Auth DB query não cacheada (50 queries/minuto no hot path)
**Arquivo:** `ErrorLogService.cs:211-229`

Cada ingest individual faz uma query em `api_keys`. Com rate limit de 50/min, isso é 50 queries/min para autenticação de uma chave que nunca muda. A chave deveria ser cacheada no `IMemoryCache` por 5 minutos após validação bem-sucedida.

---

### M-05 — Batch swallows exceptions e retorna 200 com lista vazia
**Arquivo:** `ErrorLogService.cs:79-87`

```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "Erro ao processar log do batch: {App} - {Msg}", log.AppName, log.Message);
    // Log é silenciosamente descartado
}
return Result.Success<IReadOnlyList<IngestResponse>>(results); // results pode ser []
```

Se o banco estiver indisponível, todo o batch retorna `HTTP 200 { results: [] }`. O app cliente interpreta como sucesso e descarta os logs. Deveria retornar `HTTP 503` ou incluir `failedCount` na resposta.

---

### M-06 — Validação básica do batch ocorre APÓS query ao banco
**Arquivo:** `ErrorLogService.cs:60-73`

```csharp
var authResult = await ValidateApiKeyAsync(apiKeyPlain, ct); // ← DB query cara
// ...
if (request.Logs == null || request.Logs.Count == 0)  // ← deveria ser primeiro
```

Validações baratas (null check, count check, size check) devem acontecer antes da query de autenticação.

---

### M-07 — GetStatsAsync dispara 4 queries separadas (não atômico)
**Arquivo:** `ErrorLogRepository.cs:65-81`

```csharp
var totalToday     = await _db.ErrorLogs.CountAsync(...);  // Query 1
var criticalToday  = await _db.ErrorLogs.CountAsync(...);  // Query 2
var unresolvedTotal = await _db.ErrorLogs.CountAsync(...); // Query 3
var byApp = await _db.ErrorLogs.GroupBy(...).ToListAsync(); // Query 4
```

4 round trips, stats inconsistentes entre si (inserções ocorrem entre queries). Para um dashboard de monitoramento, isso pode mostrar dados contraditórios (ex: `totalToday=5` mas `byApp` soma 6). Uma CTE ou uma única query agregada seria mais eficiente e consistente.

---

### M-08 — Sem logging em casos de auth failure e rate limit
**Arquivo:** `ErrorLogService.cs:211-246`

Tentativas com API Key inválida não geram log. Impossível detectar:
- Keys mal configuradas em apps externas
- Tentativas de brute force
- Qual app está sendo rate-limited

**Correção:**
```csharp
_logger.LogWarning("API Key inválida recebida para error-log ingest. KeyHash prefix: {Prefix}", 
    keyHash[..8]); // nunca logar a key completa
```

---

### M-09 — Fingerprint computado ANTES do scrubbing
**Arquivo:** `ErrorLogService.cs:174-175`

```csharp
var fingerprint = ErrorLog.ComputeFingerprint(req.AppName, req.ExceptionType, req.Source, req.LineNumber);
// O scrubbing de ExceptionType/Source não ocorre (esses campos não são scrubados — ver A-04)
// Mas se fossem scrubados, o fingerprint deveria usar os valores originais para consistência
```

O fingerprint usa os valores originais de `ExceptionType` e `Source` (não scrubados). Isso é correto para deduplicação, mas se PII aparecer no ExceptionType (ex: `UserNotFoundException: email=user@test.com`), o fingerprint codifica o erro genérico (correto) enquanto o ExceptionType armazenado deve ter PII removida (que não ocorre — ver A-04).

---

### M-10 — Stats usa UTC midnight, não local (timezone Brasil)
**Arquivo:** `ErrorLogRepository.cs:67`

```csharp
var todayUtc = DateTime.UtcNow.Date; // Meia-noite UTC = 21h do dia anterior no Brasil (UTC-3)
```

Para usuário em São Paulo, "hoje" começa às 00:00 BRT (03:00 UTC). As stats de "hoje" às 23:00 BRT incluiriam apenas 20 horas de erros, não 23. Pequeno para um dashboard interno, mas relevante para alertas.

---

## 🔵 PROBLEMAS BAIXOS

---

### B-01 — GUID PK gera fragmentação no clustered index
**Arquivo:** `ErrorLog.cs:35` (herda `Entity` que usa `Guid.NewGuid()`)

`Guid.NewGuid()` produz GUIDs aleatórios. SQL Server insere em páginas aleatórias do B-tree → fragmentação crescente → rebuild periódico necessário. A 500 erros/dia (180k/ano), o impacto é gerenciável mas não ideal para shared hosting.

**Projeção de crescimento da tabela:**
- 3 meses: ~45k rows, ~15MB (estimativa com stack traces avg 2KB)
- 6 meses: ~90k rows, ~30MB
- 12 meses: ~180k rows, ~60MB
- Retenção de 90 dias limita a ~45k rows em steady state

Shared hosting SmarterASP tipicamente limita banco a 200MB-1GB → sem risco imediato.

---

### B-02 — DeleteOlderThanAsync sem delay entre batches
**Arquivo:** `ErrorLogRepository.cs:83-100`

O loop de delete em lotes de 500 não tem pausa entre iterações. Em tabela com 45k rows e 90 dias de retenção, uma limpeza completa faz ~90 DELETE em lote, potencialmente travando a tabela por segundos em shared hosting.

**Correção:** Adicionar `await Task.Delay(100, ct)` entre batches.

---

### B-03 — Key lookups nos índices (INCLUDE ausente)
**Arquivo:** `ErrorLogConfiguration.cs:35-41`

O índice principal `IX_error_logs_app_level_resolved_date` não tem colunas INCLUDE. A query de listagem precisa de `Message`, `ExceptionType`, `OccurredAt`, `OccurrenceCount` que não estão no índice → key lookup por linha retornada (até 100 lookups por página). Para 45k rows e 100 por page, isso é gerenciável mas subótimo.

**Correção:** Adicionar INCLUDE com colunas frequentemente retornadas na listagem.

---

### B-04 — Rate limiter resetado em app pool recycle
**Arquivo:** `ErrorLogService.cs:232-246`

`IMemoryCache` é in-process. SmarterASP.NET recicla o app pool periodicamente (timeout de inatividade, deploy, etc.). Após recycle, o contador de rate limit zera. Uma aplicação pode acumular burst de logs logo após recycles.

---

### B-05 — CPF regex com falsos positivos
**Arquivo:** `ErrorLogService.cs:268-269`

```regex
\d{3}[\.\-]?\d{3}[\.\-]?\d{3}[\-]?\d{2}
```

Falsos positivos em:
- Versões de biblioteca: `1.0.0.01` → match parcial
- IDs de produto: `123456789-01`
- Alguns IPs com portas: `192.168.001.01`

Isso pode redact informações legítimas de debug (versão do módulo, IDs de sessão).

---

### B-06 — `RateBucket` private impede testes unitários
**Arquivo:** `ErrorLogService.cs:280`

```csharp
private sealed class RateBucket { public int Count { get; set; } }
```

A lógica de rate limiting está embutida em uma classe privada interna, não mockável. Testes unitários de `ErrorLogService` precisariam de um `IMemoryCache` real, tornando o teste um teste de integração implícito.

---

### B-07 — `ComputeFingerprint` no Domain usa criptografia
**Arquivo:** `ErrorLog.cs:114-122`

Operações criptográficas (`SHA256`) dentro de uma entidade de domínio são incomuns e criam dependência de `System.Security.Cryptography` no projeto `Diax.Domain`. Seria mais limpo como método estático de um serviço de domínio separado (`ErrorLogFingerprintService`) ou no Application layer.

---

### B-08 — Migration com timestamp artificial
**Arquivo:** `20260605120000_AddErrorLogs.cs`

O timestamp `120000` (meio-dia) é artificial. O EF Core usa timestamps reais nos nomes de migration. Se futura migration for gerada automaticamente, ela terá timestamp real e virá depois cronologicamente (correto). Porém, o ModelSnapshot foi atualizado manualmente e pode divergir da realidade se o `dotnet ef` não puder ser executado para verificação.

---

## Análise de Testabilidade

### O que DEVE ter testes unitários (não tem nenhum):
| Componente | Motivo |
|-----------|--------|
| `ScrubPii()` | Regex complexa com edge cases (CNPJ vs CPF, Unicode) |
| `TruncateUtf8()` | Bug C-03 seria pego aqui |
| `ComputeFingerprint()` | Cobre colisões e edge cases (null, empty) |
| `CheckRateLimit()` | Race condition A-01 seria detectado com testes paralelos |
| `ValidateApiKeyAsync()` | Scope bypass A-03 seria pego aqui |
| Cursor pagination | Bug C-04 seria imediato |

### O que DEVE ter testes de integração:
| Fluxo | Motivo |
|-------|--------|
| Ingest + dedupe concorrente | Race condition C-02 |
| Resolve + verificação no banco | Bug C-01 seria imediato |
| Batch com DB down | Comportamento atual: 200 + lista vazia |
| Rate limit sob carga | Race condition A-01 |

**Cobertura mínima recomendada:** 80% de branches em `ErrorLogService`, 100% nos regex de scrubbing.

---

## Compatibilidade com Produção

### SmarterASP.NET
- ✅ Sem dependências de Linux
- ✅ SQL Server compatível
- ⚠️ App pool recycle invalida rate limiter (B-04)
- ⚠️ Shared hosting: pool de conexões SQL limitado — 4 queries em GetStatsAsync em paralelo pode saturar

### GitHub Actions / Deploy
- ✅ Nenhuma mudança no pipeline necessária
- ⚠️ Migration aplicada manualmente (SQL direto) — `update-db.ps1` ainda usa `dotnet ef` quebrado
- ⚠️ ModelSnapshot atualizado manualmente — futura migration automática pode divergir

---

## Sumário Executivo

| # | ID | Título | Severidade | Prob. |
|---|---|--------|------------|-------|
| 1 | C-01 | Resolve nunca persiste | **Crítica** | 100% |
| 2 | C-02 | Race condition no dedupe | **Crítica** | Alta |
| 3 | C-03 | TruncateUtf8 corrompe Unicode | **Crítica** | Média |
| 4 | C-04 | Cursor pagination quebrado | **Crítica** | 100% |
| 5 | A-01 | Rate limiter não thread-safe | **Alta** | Alta |
| 6 | A-02 | RecordUsage nunca salvo | **Alta** | 100% |
| 7 | A-03 | Scope check com substring bypass | **Alta** | Baixa |
| 8 | A-04 | PII sem scrubbing em 4 campos | **Alta** | Média |
| 9 | A-05 | OccurredAt sem validação | **Alta** | Baixa |
| 10 | A-06 | AppName não vinculado à Key | **Alta** | Baixa |
| 11 | A-07 | Hash duplo da API Key | **Média-Alta** | 100% |
| 12 | M-01 | Level ausente no fingerprint | **Média** | 100% |
| 13 | M-02 | Environment ausente no fingerprint | **Média** | 100% |
| 14 | M-03 | AdditionalData double serialization | **Média** | Média |
| 15 | M-04 | Auth query não cacheada | **Média** | 100% |
| 16 | M-05 | Batch 200 com lista vazia em falha | **Média** | Baixa |
| 17 | M-06 | Validação antes de auth invertida | **Média** | 100% |
| 18 | M-07 | GetStats: 4 queries não atômicas | **Média** | 100% |
| 19 | M-08 | Sem log em auth failure/rate limit | **Média** | 100% |
| 20 | M-09 | Fingerprint antes do scrubbing | **Média** | Baixa |
| 21 | M-10 | Stats usa UTC midnight (BR ≠ UTC) | **Média** | 100% |
| 22 | B-01 | GUID PK fragmentação | **Baixa** | Alta |
| 23 | B-02 | Delete sem delay entre batches | **Baixa** | Baixa |
| 24 | B-03 | Key lookups por ausência de INCLUDE | **Baixa** | Alta |
| 25 | B-04 | Rate limit reseta em app pool recycle | **Baixa** | Média |
| 26 | B-05 | CPF regex falsos positivos | **Baixa** | Média |
| 27 | B-06 | RateBucket não mockável | **Baixa** | 100% |
| 28 | B-07 | SHA256 no Domain layer | **Baixa** | 100% |
| 29 | B-08 | Migration timestamp artificial | **Baixa** | Baixa |

---

## Recomendação

**Corrigir obrigatoriamente antes do frontend:** C-01, C-02, C-03, C-04, A-01, A-04

**Corrigir antes de integrar apps externas:** A-02, A-03, A-05, A-06

**Corrigir antes de produção plena:** M-01 a M-10

**Aceitar como dívida técnica conhecida:** B-01 a B-08 (nenhum impede operação)
