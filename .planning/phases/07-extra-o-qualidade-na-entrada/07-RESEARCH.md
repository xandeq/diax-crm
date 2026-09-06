# Phase 7: Extração — Qualidade na Entrada - Research

**Researched:** 2026-09-05
**Domain:** DNS MX validation (.NET), EF Core migration coordination, persistent cache design on shared hosting
**Confidence:** HIGH (DnsClient.NET internals, EF Core patterns) / MEDIUM (SmarterASP DNS egress — unverifiable, mitigated by design)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Checagem de MX (EXTR-01)**
- **D-01:** Usar a lib **DnsClient.NET** (NuGet) para query MX de verdade. Decidido contra shell-out para `nslookup` (frágil em shared hosting/IIS) e contra checar só A/AAAA via `System.Net.Dns` (sinal fraco demais).
- **D-02:** **Falha/timeout de DNS não rejeita o lead.** Lead passa e é registrado como "MX não verificado". Falha de infraestrutura não é prova de lead ruim.
- **D-03:** **Cache persistente por domínio com TTL**, espelhando o `mx-cache.json` (30 dias) do Python. Forma exata do storage fica a critério do planner.

**Registro de rejeição (EXTR-02)**
- **D-04:** **Agregado por rodada na entidade `CustomerImport`** que já existe: contadores por motivo (geo fora do alvo / e-mail lixo / sem MX / duplicado) por execução do import. Consultável por período via `CreatedAt`.
- **D-05:** **Decidido contra tabela de rejeição por lead** (banco em 467/500MB de quota; ~900 leads/dia rejeitados hoje). Aceita-se perder "qual e-mail foi rejeitado por quê" em troca de custo zero de storage.

**Persistência da classificação de site (EXTR-03)**
- **D-06:** Coluna nova em `Customer` — `WebsiteKind` (Unknown / OwnSite / Directory).
- **D-07:** **A migration desta coluna sai JUNTO com a coluna `ExternalId` da Phase 8, numa migration única.** Implicação: criar a migration durante a Phase 7 contendo AMBAS as colunas; Phase 8 apenas consome `ExternalId`.
- **D-08:** O critério de "diretório de terceiro" reaproveita `DIRECTORY_HOSTS` de `docs/email-marketing/site_check.py` — não reinventar a lista.

**Destino da ponte Python**
- **D-09:** Aposentar a ponte manual assim que o `ExtractorPullWorker` for validado em produção. `import_extrator_bridge.py` permanece como fallback de emergência.
- **D-10:** ⚠️ D-09 depende de verificação externa da primeira execução real do worker (fora do escopo de código desta fase) — D-01..D-08 seguem válidas independentemente.

### Claude's Discretion
- Forma exata do cache de MX (tabela vs arquivo vs IMemoryCache + persistência)
- Timeout e política de retry da query DNS
- Como os contadores por motivo são serializados no `CustomerImport` (colunas novas vs JSON em `ErrorDetails`)
- Paralelismo/batching das queries DNS durante o import

### Deferred Ideas (OUT OF SCOPE)
- Google Places API como fonte alternativa de leads (EXTR-04, v2)
- Backfill dos leads já no banco que falhariam nos novos filtros (candidato a fase própria)
- Verificação SMTP real (handshake) — fora de escopo, risco de reputação
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| EXTR-01 | Sistema rejeita lead cujo domínio de email não tem MX/A válido antes de importar | DnsClient.NET API verificada (LookupClient, QueryType.MX/A, NXDOMAIN vs timeout, Null MX RFC 7505) — ver Code Examples |
| EXTR-02 | Sistema registra o motivo de rejeição, consultável por período | `CustomerImport` já tem `CreatedAt` indexado; gap identificado = contadores por motivo nunca são persistidos hoje (só logados) — ver Architecture Patterns |
| EXTR-03 | Sistema classifica `website` como site próprio vs diretório de terceiro | `DIRECTORY_HOSTS`/`classify_url` do `site_check.py` são portáveis 1:1 (função pura, sem I/O) — ver Code Examples |
</phase_requirements>

## Summary

A pesquisa confirma que os três requisitos são implementáveis com baixo risco técnico, usando padrões já estabelecidos no codebase (filtros privados em `ExtractorIntegrationService`, `IEntityTypeConfiguration<T>` para EF Core, testes com `CreateSut(options)` + Moq). O ponto técnico mais denso é a checagem de MX: `DnsClient.NET` 1.8.0 (verificado via NuGet — última versão estável, publicada 2024, sem release mais recente) expõe exatamente o mecanismo que a decisão D-02 precisa — **timeout sempre lança `DnsResponseException` com `Code == DnsResponseCode.ConnectionTimeout`, enquanto NXDOMAIN e outros erros de protocolo DNS voltam como `response.HasError`/`response.Header.ResponseCode` (não lançam exceção, por padrão `ThrowDnsErrors=false`)** — confirmado lendo o código-fonte da tag `1.8.0` no GitHub, não apenas a doc. Essa distinção é exatamente o que permite codificar D-02 sem ambiguidade: `catch (DnsResponseException ex) when (ex.Code == DnsResponseCode.ConnectionTimeout)` → "MX não verificado"; resposta sem exceção e sem `MxRecord` → domínio realmente sem MX → candidato a rejeição (depois do fallback para A, replicando o Python).

O segundo ponto denso é EXTR-02: a entidade `CustomerImport` **já** tem os campos certos para agregação (`CreatedAt` indexado, `SuccessCount`/`FailedCount`), mas os contadores por motivo (geo, e-mail lixo, sem MX, duplicado) **hoje só existem como variáveis locais logadas** em `ExtractorIntegrationService.ImportLeadsAsync` — nunca chegam a `CustomerImport`. Isso não é um bug do EXTR-02, é o próprio gap que a fase resolve, mas tem uma implicação arquitetural que o planner precisa decidir explicitamente: os filtros de qualidade rodam em `ExtractorIntegrationService` (antes de `CustomerImportService.ImportAsync`), mas é o `CustomerImportService` quem cria e persiste o `CustomerImport`. Os contadores precisam atravessar essa fronteira — a forma mais simples é estender `BulkImportRequest` com um objeto de contagens pré-computadas que `CustomerImportService` grava no mesmo `CustomerImport` que já cria.

O terceiro achado importante: `DIRECTORY_HOSTS` e `classify_url` em `site_check.py` são **funções puras sem I/O** (só `urlparse` + substring match) — a porta para C# é literalmente 1:1, sem nenhuma das complicações de rede/SSRF que o resto do arquivo tem (aquilo é para o diagnóstico de site completo, fora de escopo aqui).

Sobre o risco de DNS bloqueado em shared hosting (pergunta 7): não há documentação pública confirmando ou negando bloqueio de UDP/53 de saída no SmarterASP.NET. A mitigação já está embutida na própria decisão D-02: se o DNS estiver de fato bloqueado, toda query cairá em `ConnectionTimeout`, e o sistema **degrada graciosamente para "MX não verificado" em 100% dos casos** em vez de rejeitar leads em massa — o pior cenário é "a feature não filtra nada", não "a feature quebra o import". Ainda assim, recomenda-se instrumentação (log de warning se a taxa de MX-verificado cair a zero numa rodada) para detectar isso cedo.

**Primary recommendation:** Portar a checagem de MX com `DnsClient.NET` 1.8.0 atrás de uma interface própria (`IMxLookupService`) testável offline via Moq; persistir o cache em uma tabela EF nova e enxuta (não `IMemoryCache`, que não sobrevive a reciclagem do App Pool no IIS); adicionar 4 colunas de contador a `CustomerImport` (não JSON em `ErrorDetails`, que é per-row e caro); criar a migration única (WebsiteKind + ExternalId) imediatamente após esta pesquisa, antes de qualquer outra sessão tocar `DiaxDbContextModelSnapshot.cs`.

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| DnsClient.NET | **1.8.0** (verificado via NuGet registry em 2026-09-05 — última estável, sem beta mais nova) | Query MX/A real via socket UDP/TCP, com retry/timeout configuráveis | Já é a lib de fato-padrão do ecossistema .NET para DNS além do `System.Net.Dns` (que só resolve A/AAAA e não expõe MX); mantida ativamente (releases 2024, MIT/Apache 2.0) |

**Installation:**
```bash
cd api-core
dotnet add src/Diax.Application package DnsClient --version 1.8.0
```
Adicionar em `Diax.Application.csproj` (mesmo padrão dos outros pacotes já listados lá — a lib não faz I/O de arquivo/HTTP framework-specific, cabe na camada Application como as outras libs "utilitárias" hoje: `HtmlAgilityPack`, `HtmlSanitizer`).

**Version verification:** confirmado via `curl https://api.nuget.org/v3-flatcontainer/dnsclient/index.json` — versões disponíveis vão até `1.8.0` (estável) e `1.8.0-beta-*` (pré-release da mesma versão, não mais nova). Nenhuma versão 2.x existe. HIGH confidence (fonte primária: NuGet API).

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| DnsClient.NET | `System.Net.Dns.GetHostAddresses` | Só A/AAAA, sem MX — já rejeitado por D-01 |
| DnsClient.NET | `Process.Start("nslookup")` | Frágil/pode estar bloqueado em shared hosting — já rejeitado por D-01 |
| DnsClient.NET | `ARSoft.Tools.Net` (outra lib DNS .NET) | Também válida, mas sem tração equivalente no ecossistema .NET moderno; não pesquisada a fundo pois D-01 já travou DnsClient.NET |

## Architecture Patterns

### Recommended Project Structure

Seguir o padrão já estabelecido (filtros privados dentro do service que já orquestra o loop), mas extrair o MX check para uma interface própria — diferente dos filtros `IsLowQualityEmail`/`IsOutsideTargetGeo`, que são métodos privados síncronos e puros, o MX check é **assíncrono e faz I/O de rede**, o que exige um seam testável:

```
Diax.Application/Customers/
├── ExtractorIntegrationService.cs      # loop por lead — chama IMxLookupService + IWebsiteClassifier
├── Services/
│   ├── IMxLookupService.cs             # NOVO — interface (testável via Moq)
│   └── MxLookupService.cs (impl fica em Infrastructure, ver abaixo)
├── WebsiteClassification/
│   └── WebsiteClassifier.cs            # NOVO — porta pura de classify_url/DIRECTORY_HOSTS, sem interface (função estática, sem I/O)

Diax.Infrastructure/
├── Dns/
│   └── DnsClientMxLookupService.cs     # NOVO — implementação real com LookupClient
└── Data/
    ├── Configurations/
    │   └── MxCacheEntryConfiguration.cs # NOVO — se optar por tabela EF (recomendado)
```

`WebsiteClassifier` **não precisa de interface** — é função pura sem I/O (mesmo raciocínio de `classify_url` em Python: `urlparse` + substring match, zero dependência externa). Só o MX lookup precisa de seam por causa do I/O de rede.

### Pattern 1: Interface própria para I/O de rede testável offline

**What:** `IMxLookupService` na Application, implementação real com `LookupClient` na Infrastructure — mesmo padrão de `IExtractorService`/`ICustomerRepository` já usados em `ExtractorIntegrationService`.
**When to use:** Sempre que o filtro precisa fazer I/O real (rede/DB) e os testes existentes (`ExtractorIntegrationServiceTests`, padrão `CreateSut(options)` + Moq) precisam continuar rodando sem rede.
**Example:**
```csharp
// Diax.Application/Customers/Services/IMxLookupService.cs
namespace Diax.Application.Customers.Services;

public enum MxCheckResult
{
    /// <summary>MX (ou A como fallback RFC 5321) resolvido com sucesso.</summary>
    Valid,
    /// <summary>NXDOMAIN, Null MX (RFC 7505) ou resposta vazia sem A — domínio comprovadamente sem entrega.</summary>
    NoMx,
    /// <summary>Timeout/falha de infraestrutura DNS — D-02: NÃO rejeita o lead.</summary>
    Unverified
}

public interface IMxLookupService
{
    Task<MxCheckResult> CheckAsync(string domain, CancellationToken cancellationToken = default);
}
```

```csharp
// Diax.Infrastructure/Dns/DnsClientMxLookupService.cs
using DnsClient;

public class DnsClientMxLookupService : IMxLookupService
{
    private readonly ILookupClient _lookup;

    public DnsClientMxLookupService(ILookupClient lookup) => _lookup = lookup;

    public async Task<MxCheckResult> CheckAsync(string domain, CancellationToken ct = default)
    {
        try
        {
            var mxResult = await _lookup.QueryAsync(domain, QueryType.MX, cancellationToken: ct);
            var mxRecords = mxResult.Answers.MxRecords().ToList();

            // Null MX (RFC 7505): único registro MX com Exchange == "." → sem serviço de email.
            var isNullMx = mxRecords.Count == 1 && mxRecords[0].Exchange.Value == ".";
            if (mxRecords.Count > 0 && !isNullMx)
                return MxCheckResult.Valid;

            if (mxResult.HasError &&
                (DnsResponseCode)mxResult.Header.ResponseCode == DnsResponseCode.NotExistentDomain)
                return MxCheckResult.NoMx; // NXDOMAIN — domínio não existe

            // Sem MX (vazio ou Null MX): RFC 5321 permite fallback para registro A do domínio.
            var aResult = await _lookup.QueryAsync(domain, QueryType.A, cancellationToken: ct);
            return aResult.Answers.ARecords().Any() ? MxCheckResult.Valid : MxCheckResult.NoMx;
        }
        catch (DnsResponseException ex) when (ex.Code == DnsResponseCode.ConnectionTimeout)
        {
            // D-02: falha de infraestrutura NÃO rejeita — passa como "não verificado".
            return MxCheckResult.Unverified;
        }
    }
}
```
Registro em `DependencyInjection.cs` (Infrastructure):
```csharp
services.AddSingleton<ILookupClient>(_ => new LookupClient(new LookupClientOptions
{
    Timeout = TimeSpan.FromSeconds(3),   // discretion: mais curto que o default (5s) — ver Common Pitfalls
    Retries = 1,                          // discretion: default é 2; reduzir para não estourar o BackgroundService
    UseCache = true,                      // cache em memória do próprio DnsClient (TTL do registro DNS) — complementa, não substitui, o cache persistente D-03
    ThrowDnsErrors = false                // default — mantém NXDOMAIN como resposta, não exceção (necessário p/ distinguir de timeout)
}));
services.AddScoped<IMxLookupService, DnsClientMxLookupService>();
```
`LookupClient` é thread-safe e caro de construir (mantém sockets/handlers) — **deve ser singleton**, igual ao guidance oficial da lib (mesmo padrão de `HttpClient`).

Fonte: código-fonte oficial `MichaCo/DnsClient.NET`, tag `1.8.0`, arquivos `LookupClient.cs` (linhas 831, 876, 1109, 1154, 1195-1204 confirmam o comportamento timeout-sempre-lança vs erro-de-protocolo-gated-por-ThrowDnsErrors), `DnsResponseCode.cs` (enum com `ConnectionTimeout = 999`, valor sintético da própria lib — não é um código DNS real, é o sinal que a lib usa para "não consegui nem falar com nenhum servidor"), `DnsString.cs` (linha 34: `RootLabel = new DnsString(".", ".")`, confirma que Null MX vira `Exchange.Value == "."`). HIGH confidence — lido diretamente do código da tag de release, não apenas da documentação.

### Pattern 2: Cache persistente por domínio (D-03)

**What:** Tabela EF nova, não `IMemoryCache`.
**Why not IMemoryCache:** já é usado no projeto (`ConfigurationProvider.cs`, TTL de 5 min) mas é **em processo** — some a cada reciclagem do App Pool do IIS, que em shared hosting acontece com frequência (idle timeout, deploy, memory pressure). Um cache de 30 dias que reseta a cada reciclagem não cumpre D-03 ("evita ~1000 queries DNS/dia re-consultando os mesmos domínios").
**Why not arquivo (espelhando `mx-cache.json`):** o projeto não tem nenhum padrão de escrita em disco para dado durável — a única escrita em `App_Data` hoje é `startup-error.txt`, um artefato de diagnóstico, não um dado de negócio. Múltiplas instâncias/deploys via FTP tornariam um arquivo local frágil (sobrescrito no próximo deploy, não teria dedup entre instâncias se o plano de hosting escalar).
**Why tabela EF:** único mecanismo de persistência durável já estabelecido no projeto (SQL Server via `update-db.ps1`, mesma disciplina de todo o resto do sistema). Volume é pequeno e previsível: domínios únicos vistos pelo Extrator, não linhas por lead — ordem de grandeza de milhares de linhas, não centenas de milhares (D-05 já mediu ~900 leads/dia rejeitados; domínios únicos são uma fração disso). Comparar com `audit_logs` (279MB): essa tabela nunca vai chegar perto disso.

**Recommended schema:**
```csharp
// Diax.Domain/Customers/MxCacheEntry.cs
public class MxCacheEntry : AuditableEntity   // reaproveita CreatedAt/UpdatedAt já existentes no padrão
{
    public string Domain { get; private set; } = string.Empty;   // chave lógica, lowercase, sem '@'
    public MxCheckResult Result { get; private set; }             // Valid / NoMx / Unverified
    public DateTime CheckedAt { get; private set; }

    protected MxCacheEntry() { }
    public MxCacheEntry(string domain, MxCheckResult result)
    {
        Domain = domain;
        Result = result;
        CheckedAt = DateTime.UtcNow;
    }
    public void Refresh(MxCheckResult result) { Result = result; CheckedAt = DateTime.UtcNow; }
}
```
```csharp
// Configuration — HasIndex único em Domain, TTL avaliado em código (CheckedAt < DateTime.UtcNow.AddDays(-30))
builder.HasIndex(x => x.Domain).IsUnique().HasDatabaseName("IX_MxCacheEntries_Domain");
```
Nota: `Result == Unverified` **também deve ser cacheado** (com TTL provavelmente mais curto que 30 dias, ex. 1 dia) para não martelar o mesmo domínio problemático a cada rodada — mas isso é decisão de afinamento, não bloqueia o design.

### Pattern 3: Contadores de rejeição atravessando a fronteira Application (D-04)

**What:** `ExtractorIntegrationService` computa os 4 contadores (geo, e-mail lixo, sem MX, duplicado) **antes** de chamar `CustomerImportService.ImportAsync`, mas é `CustomerImportService` quem cria e persiste o `CustomerImport`. Hoje (`ExtractorIntegrationService.cs` linhas 80-172) os contadores `skippedByGeo`/`rejectedLowQuality` já existem como variáveis locais — só viram `_logger.LogInformation`, nunca persistem.
**Gap concreto a resolver:** estender `BulkImportRequest` (em `BulkImportDtos.cs`) com um objeto de pré-contagens, e `CustomerImportService.ImportAsync` grava esses valores no mesmo `import` que já cria (linha 315: `new CustomerImport(fileName, ImportType.CSV, request.Customers.Count)`).
**Example:**
```csharp
// BulkImportDtos.cs — adicionar
public record ImportRejectionCounts(
    int GeoRejected = 0,
    int LowQualityEmailRejected = 0,
    int NoMxRejected = 0,
    int DuplicateRejected = 0);

public record BulkImportRequest(
    List<ImportCustomerRow> Customers,
    LeadSource Source = LeadSource.Import,
    bool DryRun = false,
    ImportRejectionCounts? RejectionCounts = null);   // novo, opcional — só Extractor preenche
```
```csharp
// CustomerImport.cs — novo método de domínio, chamado antes de Complete()
public void RecordRejectionCounts(int geo, int lowQualityEmail, int noMx, int duplicate)
{
    GeoRejectedCount = geo;
    LowQualityEmailRejectedCount = lowQualityEmail;
    NoMxRejectedCount = noMx;
    DuplicateRejectedCount = duplicate;
}
```
**Por que colunas novas, não JSON em `ErrorDetails`:** `ErrorDetails` já tem um propósito estabelecido — lista **por linha** de erros de validação (`ImportError(RowNumber, Email, ErrorMessage)`), usado hoje só no caminho `LeadSource.Import` (CSV manual). Misturar contadores agregados nesse campo exigiria parsear/reparsear JSON toda vez que alguém quiser "quantos rejeitados por sem-MX este mês" — uma query SQL direta em 4 colunas `int` é ordens de magnitude mais simples e é exatamente o padrão que `SuccessCount`/`FailedCount` já estabelecem na mesma entidade. Consultável por período via o índice `IX_CustomerImports_CreatedAt` que já existe.

### Pattern 4: Classificação de site (EXTR-03) — porta 1:1

**What:** `classify_url`/`DIRECTORY_HOSTS` são funções puras (sem rede, sem SSRF-guard necessário — isso só existe no resto de `site_check.py` para o diagnóstico completo, fora de escopo aqui).
**Example:**
```csharp
// Diax.Application/Customers/WebsiteClassification/WebsiteClassifier.cs
public static class WebsiteClassifier
{
    // Porta 1:1 de DIRECTORY_HOSTS em site_check.py — D-08: não reinventar a lista.
    private static readonly string[] DirectoryHosts =
    {
        "econodata", "cliniguia", "facebook.", "instagram.", "linkedin.", "google.", "goo.gl",
        "apontador", "guiamais", "telelistas", "solutudo", "cnpj.biz", "cnpj.info", "consultacnpj",
        "empresascnpj", "casadosdados", "yelp.", "tripadvisor", "ifood", "doctoralia", "boaconsulta",
        "jusbrasil", "olx.", "mercadolivre", "shopee", "wa.me", "whatsapp.", "bit.ly", "linktr.ee",
        "youtube.", "tiktok.", "kekanto", "hotmart", "lojaintegrada", "wixsite", "site123",
        "negocio.site", "business.site", "nuvemshop", "blogspot", "wordpress.com"
    };

    public static WebsiteKind Classify(string? url)
    {
        var u = (url ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(u) || u is "-" or "n/a" or "null" or "none")
            return WebsiteKind.Unknown;

        var host = new Uri(u.Contains("://") ? u : "http://" + u, UriKind.Absolute).Host;
        // Uri lança se malformado — cair em Unknown é o comportamento correto (mesma semântica do
        // Python: host vazio ou sem '.' => 'none').
        if (string.IsNullOrEmpty(host) || !host.Contains('.'))
            return WebsiteKind.Unknown;

        return DirectoryHosts.Any(host.Contains) ? WebsiteKind.Directory : WebsiteKind.OwnSite;
    }
}
```
Cuidado: `new Uri(...)` pode lançar `UriFormatException` para entradas realmente malformadas (Python não lança, só retorna `'none'`) — envolver em `try/catch` retornando `Unknown`, para manter paridade de comportamento (nunca deixar uma URL esquisita quebrar o import).

### Anti-Patterns to Avoid
- **Rejeitar lead por timeout de DNS:** viola D-02 diretamente. O `catch` de `DnsResponseException` precisa checar `ex.Code == DnsResponseCode.ConnectionTimeout` especificamente — outros `DnsResponseException` (se `ThrowDnsErrors` for acidentalmente ligado) não devem ser tratados como "não verificado" sem revisão.
- **Guardar contadores de rejeição como JSON solto:** quebra a garantia de "consultável por período" com SQL simples — ver Pattern 3.
- **`LookupClient` novo a cada chamada:** caro (aloca sockets), deve ser singleton via DI — mesmo raciocínio de não instanciar `HttpClient` por request.
- **Aplicar o MX check em todo `CustomerImportService.ImportAsync`:** fora de escopo — CONTEXT.md e canonical_refs deixam claro que o ponto de inserção é o loop do `ExtractorIntegrationService`, não o service genérico de import (que também atende `LeadSource.Import`/CSV manual, com sua própria validação de e-mail já madura).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Parsing de resposta DNS raw (bytes/wire format) | Parser manual de pacote UDP DNS | `DnsClient.NET` (`LookupClient.QueryAsync`) | É exatamente o problema que a lib resolve — parsing de RR, TCP fallback em truncamento, retry/timeout, tudo testado pela comunidade |
| Detecção de "Null MX" (RFC 7505) | Regex ad-hoc no texto da resposta | Comparar `MxRecord.Exchange.Value == "."` (ou `.Equals(DnsString.RootLabel)`) | A lib já expõe o dado estruturado — não há razão para re-parsear texto como o Python faz via `nslookup` |
| Lista de diretórios de terceiros | Nova heurística/lib de classificação de URL | Portar `DIRECTORY_HOSTS` literal (D-08) | Já validada em produção pelo `site_check.py`; reinventar introduziria divergência de comportamento entre o pipeline de email e o de import |

**Key insight:** o único componente genuinamente novo desta fase é o cliente DNS — todo o resto (classificação de site, agregação de contadores) é composição de padrões já existentes no codebase (`IEntityTypeConfiguration<T>`, `Result<T>`, filtros privados sequenciais no loop de import).

## Common Pitfalls

### Pitfall 1: Confundir "sem resposta" com "domínio sem MX"
**What goes wrong:** tratar qualquer falha (timeout, erro de rede, NXDOMAIN) da mesma forma, rejeitando leads por instabilidade momentânea do DNS.
**Why it happens:** `DnsClient.NET`, com `ThrowDnsErrors=false` (default), devolve **ambos** os casos sem lançar exceção às vezes — mas timeout **sempre** lança `DnsResponseException(ConnectionTimeout)` independente do flag (confirmado no código-fonte), enquanto NXDOMAIN só aparece como exceção se `ThrowDnsErrors=true`. Misturar os dois caminhos de tratamento quebra D-02.
**How to avoid:** dois caminhos de código separados e explícitos — `catch (DnsResponseException ex) when (ex.Code == ConnectionTimeout)` → `Unverified`; ausência de MX/A na resposta normal (sem exceção) → `NoMx`. Ver Pattern 1.
**Warning signs:** taxa de rejeição por "sem MX" subindo de forma correlacionada com horários de instabilidade de rede do host, não com a qualidade real dos leads.

### Pitfall 2: `LookupClient` com timeout default (5s) × retries (2) em 1000 leads sequenciais
**What goes vai errado:** worst case teórico de ~10s por domínio (5s × 2 tentativas) se todos os domínios novos (sem cache) travarem — em paralelismo baixo ou sequencial, um `BackgroundService` de import pode estourar tempo de execução muito além do esperado.
**Why it happens:** defaults da lib são conservadores para uso interativo, não para lote noturno de milhares de domínios.
**How to avoid:** (a) cache primeiro — a maioria dos domínios em rodadas subsequentes será hit; (b) paralelismo limitado (`Parallel.ForEachAsync`, grau ~8, mesmo valor usado em `site_check.py`'s `check_many(workers=8)` — consistência com o padrão Python já validado em produção); (c) `Timeout` mais curto (2-3s) e `Retries=1` no `LookupClientOptions` — discretion do planner, mas justificado pelo volume (até 1000 leads/rodada, `MaxPages=10 * PageSize=100`).
**Warning signs:** `ExtractorPullWorker` (agendado 15:00 UTC) começando a estourar duração perceptível ou logs de `ConnectionTimeout` em massa mesmo para domínios conhecidos como válidos (gmail.com, etc.) — sinal de que o timeout está curto demais, não de bloqueio de rede.

### Pitfall 3: Migration com duas colunas não-relacionadas numa migration só, sem coordenação com sessão paralela
**What goes wrong:** `AddAgentFoundation` (v1.2, 2026-05-29) já está aplicada em produção mas seus commits nunca foram pushados a `origin/main` — se a sessão pausada do v1.2 (Phase 2 Wave 2/3) criar novas migrations sobre essa base antes de v1.3 pushar, a ordem cronológica das migrations locais diverge da de produção.
**Why it happens:** duas sessões Claude Code trabalhando no mesmo repo/branch local em momentos diferentes, cada uma gerando migrations a partir do snapshot `DiaxDbContextModelSnapshot.cs` local — que já reflete `AddAgentFoundation` (confirmado: está na pasta `Migrations/` local, entre `AddAiChatTables` e `AddErrorLogs`).
**How to avoid:** **não é um bloqueador para criar a migration desta fase agora** — o snapshot local já inclui `AddAgentFoundation`, então `add-migration.ps1 -Name AddWebsiteKindAndExternalId` vai gerar corretamente em cima do estado atual, sem conflito. O risco é só de **push**: aplicar via `update-db.ps1` (produção) e, se for pushar para `origin/main`, fazer isso antes que a sessão v1.2 resuma e gere mais migrations — ou coordenar explicitamente com o usuário antes do push (regra já existente: "git push apenas com autorização explícita").
**Warning signs:** `dotnet ef migrations add` reclamando de "pending model changes" inesperadas, ou `DiaxDbContextModelSnapshot.cs` mostrando diffs não relacionados às colunas que você está adicionando (sinal de que o snapshot local está desalinhado com produção por outro motivo).

### Pitfall 4: `Uri` do .NET não aceita host inválido silenciosamente como o Python
**What goes wrong:** `new Uri(...)` lança `UriFormatException` para entradas como `"não é url"`, enquanto `urlparse` do Python nunca lança (retorna netloc vazio).
**Why it happens:** diferença de filosofia das duas linguagens/libs.
**How to avoid:** envolver `WebsiteClassifier.Classify` em `try/catch (UriFormatException)` retornando `Unknown` — nunca deixar um `website` malformado quebrar o loop de import inteiro.
**Warning signs:** teste de paridade com um input adversarial (ex. `"http://"`, `":::"`, string vazia) lançando exceção não tratada.

## Code Examples

Ver Architecture Patterns acima — todos os exemplos de código já incluem fonte (código-fonte oficial da tag `1.8.0` de `MichaCo/DnsClient.NET` no GitHub) e foram verificados contra o release taggeado, não a branch `dev`.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|---------------|--------|
| Checagem de MX via ponte Python (`mx_check.py`, `nslookup` shell-out, cache em arquivo JSON) | Checagem de MX nativa no worker .NET (`DnsClient.NET`, cache em tabela EF) | Esta fase (v1.3, Phase 7) | Elimina a dependência da ponte manual para qualidade de entrada; D-09 permite aposentar `import_extrator_bridge.py` assim que o worker for validado |

**Deprecated/outdated:**
- `docs/email-marketing/mx_check.py` fica como referência de comportamento (canonical ref), não como código em produção após esta fase — mas **não deve ser deletado nesta fase** (fallback de emergência per D-09, e é usado por outros scripts de email marketing que não são desta fase).

## Open Questions

1. **`is_junk_domain` (placeholders, `.local`, `wixpress`, `sentry`) não está coberto pelas decisões D-01..D-03**
   - What we know: `mx_check.py` roda `is_junk_domain` **antes** de `resolve_mx` (grátis, sem I/O) — está listado em canonical_refs como "lógica a ser portada". `wixpress.com`/`sentry.io` são domínios REAIS com MX válido (infraestrutura de terceiros que vaza como remetente default de formulário), então uma checagem de MX pura NÃO pegaria esses casos.
   - What's unclear: EXTR-01 (texto do REQUIREMENTS.md) fala só em "domínio sem MX/A válido" — não menciona placeholders/vazamento de infra de terceiros. CONTEXT.md D-01..D-03 também não mencionam essa pré-filtragem explicitamente, só o canonical_ref geral.
   - Recommendation: o planner deve decidir explicitamente se `is_junk_domain` entra nesta fase (porta direta, função pura, zero custo de I/O) ou fica para depois — dado que já está listado como lógica de referência e é trivial de portar (mesmo padrão do Pattern 4), a recomendação é incluir, mas isso deve ser uma escolha visível no plano, não implícita.

2. **DNS de saída no SmarterASP.NET (shared hosting) — bloqueio de UDP/53**
   - What we know: não há documentação pública confirmando ou negando. O host já resolve hostnames com sucesso para toda chamada HTTP de saída (AWS SM, Brevo, Extrator VPS) — mas isso pode passar pelo resolver do SO via getaddrinfo, não necessariamente por socket UDP arbitrário da aplicação. `DnsClient.NET` com `AutoResolveNameServers=true` (default) usa os mesmos servidores DNS configurados no SO do host, então deveria seguir o mesmo caminho que já funciona.
   - What's unclear: se há alguma diferença de tratamento entre resolução via APIs do SO (usada implicitamente por `HttpClient`) e um socket UDP explícito aberto pela aplicação (usado por `DnsClient.NET`).
   - Recommendation: **não bloqueia a fase** — D-02 já é o circuit breaker (timeout nunca rejeita). Adicionar um log de warning/métrica se a taxa de `Unverified` numa rodada for anormalmente alta (ex. >80% quando o histórico é <5%), para detectar bloqueio de rede cedo sem depender de confirmação prévia do provedor.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.2 + Moq 4.20.72 + EFCore.InMemory 8.0.11 (`api-core/tests/Diax.Tests`) |
| Config file | `api-core/tests/Diax.Tests/Diax.Tests.csproj` (existente, sem mudança necessária) |
| Quick run command | `dotnet test --filter "FullyQualifiedName~ExtractorIntegrationServiceTests" -c Release` |
| Full suite command | `dotnet test -c Release` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| EXTR-01 | MX válido → lead passa | unit | `dotnet test --filter "FullyQualifiedName~MxLookupServiceTests" -c Release` | ❌ Wave 0 |
| EXTR-01 | NXDOMAIN → lead rejeitado | unit | idem | ❌ Wave 0 |
| EXTR-01 | Timeout DNS → lead passa, marcado "não verificado" (D-02) | unit | idem | ❌ Wave 0 |
| EXTR-01 | Null MX (RFC 7505, `.`) → tratado como sem MX | unit | idem | ❌ Wave 0 |
| EXTR-01 | Sem MX mas com A → passa (fallback RFC 5321) | unit | idem | ❌ Wave 0 |
| EXTR-01 | Domínio em cache (< 30 dias) → não faz query DNS nova | unit | `dotnet test --filter "FullyQualifiedName~MxCacheTests" -c Release` | ❌ Wave 0 |
| EXTR-02 | Contadores de rejeição persistidos em `CustomerImport`, consultáveis por `CreatedAt` | unit + integration (InMemory DB) | `dotnet test --filter "FullyQualifiedName~CustomerImportServiceTests" -c Release` | 🟡 arquivo existe, casos novos a adicionar |
| EXTR-03 | `classify_url` equivalente — site próprio vs diretório vs none, mesma tabela-verdade do `site_check.py` | unit | `dotnet test --filter "FullyQualifiedName~WebsiteClassifierTests" -c Release` | ❌ Wave 0 |
| EXTR-01/02/03 | Fluxo completo do `ExtractorIntegrationService` com os 3 filtros novos no loop | unit (Moq de `IMxLookupService`) | `dotnet test --filter "FullyQualifiedName~ExtractorIntegrationServiceTests" -c Release` | 🟡 arquivo existe (`ExtractorIntegrationServiceTests.cs`), casos novos a adicionar |

**Testando MX sem rede real:** `IMxLookupService` é mockável via `Mock<IMxLookupService>` (mesmo padrão de `Mock<IExtractorService>` já usado em `ExtractorIntegrationServiceTests.cs`) — os testes de `ExtractorIntegrationService` nunca tocam `DnsClient.NET` de verdade. Os testes do `DnsClientMxLookupService` (implementação real) **não devem rodar contra DNS público real** de forma determinística em CI — a lib não expõe um "fake resolver" nativo; a opção mais simples é testar `DnsClientMxLookupService` só nos casos que dependem puramente de parsing (Null MX detection sobre um `MxRecord` construído manualmente em memória, sem I/O) e tratar o caminho de rede real como validação manual/smoke (`python docs/email-marketing/mx_check.py exemplo.com` já serve de tabela-verdade cruzada, já que a lógica está sendo portada dele).

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~Customers" -c Release`
- **Per wave merge:** `dotnet test -c Release`
- **Phase gate:** Full suite green + `update-db.ps1` aplicado com sucesso antes de `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `api-core/tests/Diax.Tests/Customers/WebsiteClassifierTests.cs` — tabela-verdade portada de `site_check.py`'s `DIRECTORY_HOSTS`/`classify_url` (casos: site próprio, diretório conhecido, URL vazia/malformada → Unknown)
- [ ] `api-core/tests/Diax.Tests/Customers/MxLookupServiceTests.cs` (ou local equivalente) — casos de parsing puro (Null MX, MX presente, sem MX com A fallback) construindo `IDnsQueryResponse`/`MxRecord` em memória, sem rede
- [ ] Novos casos em `ExtractorIntegrationServiceTests.cs` — MX válido/inválido/timeout via `Mock<IMxLookupService>`, contadores de rejeição propagados para `BulkImportRequest`
- [ ] Novos casos em `CustomerImportServiceTests.cs` — `RecordRejectionCounts` persiste e é lido de volta corretamente
- [ ] Framework: nenhum install novo — xUnit/Moq/EFCore.InMemory já presentes

## Sources

### Primary (HIGH confidence)
- `MichaCo/DnsClient.NET` GitHub, tag `1.8.0` — `src/DnsClient/LookupClient.cs` (linhas 831, 876, 1109, 1154, 1195-1204: comportamento timeout vs erro de protocolo), `src/DnsClient/DnsResponseCode.cs` (enum completo, `ConnectionTimeout = 999`), `src/DnsClient/DnsString.cs` (linha 34: `RootLabel`), `src/DnsClient/Protocol/MXRecord.cs` (estrutura `Preference`/`Exchange`), `src/DnsClient/IDnsQueryResponse.cs` (contrato `HasError`/`ErrorMessage`/`Header`), `src/DnsClient/DnsResponseHeader.cs` (`ResponseCode` property), `src/DnsClient/ResourceRecordCollectionExtensions.cs` (`MxRecords()` extension) — lidos via `curl` do conteúdo raw da tag, não da branch `dev`
- NuGet API (`api.nuget.org/v3-flatcontainer/dnsclient/index.json`) — lista completa de versões publicadas, confirma `1.8.0` como última estável
- Codebase local: `api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs`, `ExtractorPullOptions.cs`, `CustomerImportService.cs`, `Diax.Domain/Customers/Customer.cs`, `CustomerImport.cs`, `Diax.Infrastructure/Data/Configurations/CustomerConfiguration.cs`, `CustomerImportConfiguration.cs`, `DiaxDbContext.cs`, `ExternalServices/ConfigurationProvider.cs`, `api-core/tests/Diax.Tests/Customers/ExtractorIntegrationServiceTests.cs`, `api-core/scripts/update-db.ps1`, `add-migration.ps1` — leitura direta
- `docs/email-marketing/mx_check.py`, `docs/email-marketing/site_check.py` — leitura direta, comportamento de referência confirmado (Null MX, cache 30 dias, `DIRECTORY_HOSTS`)
- `git log`, `git branch` locais — confirma que `20260529134701_AddAgentFoundation` já está presente na pasta `Migrations/` e no snapshot do branch de trabalho `chore/email-automation-versioned`

### Secondary (MEDIUM confidence)
- `dnsclient.michaco.net/docs/` (docs oficiais, via WebFetch) — usado para confirmar defaults (`Timeout=5s`, `Retries=2`, `ThrowDnsErrors=false`), cruzado com o código-fonte (concordam)

### Tertiary (LOW confidence)
- Comportamento de firewall/DNS de saída do SmarterASP.NET shared hosting — **nenhuma fonte encontrada** confirma ou nega bloqueio de UDP/53; tratado como Open Question com mitigação de design (D-02 como circuit breaker), não como fato assumido

## Metadata

**Confidence breakdown:**
- Standard stack (DnsClient.NET): HIGH — versão verificada via NuGet API, API verificada lendo código-fonte da tag de release
- Architecture (cache, contadores, classificação): HIGH — todos os padrões espelham código já existente e revisado no próprio repo
- Pitfalls: HIGH para os técnicos (DnsClient.NET, migration order), MEDIUM para o de shared hosting (não verificável, mas mitigado por design)

**Research date:** 2026-09-05
**Valid until:** ~2026-12 (30 dias regra padrão; DnsClient.NET é lib estável de baixa cadência de release, risco de staleness baixo)
