# Phase 7: Extração — Qualidade na Entrada - Context

**Gathered:** 2026-09-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Leads de baixa qualidade são barrados **antes** de virarem `Customer`: domínio de e-mail sem MX
válido é rejeitado, o motivo de cada rejeição fica registrado e consultável por período, e o
`website` do lead é classificado como "site próprio" vs "diretório de terceiro" e persistido.

Requisitos: EXTR-01 (MX check no worker .NET), EXTR-02 (motivo de rejeição registrado),
EXTR-03 (classificação site-próprio-vs-diretório).

Fora desta fase: dedup por `ExternalId`, fix do dedup de `source=Scraping`, cálculo de score no
import — tudo isso é Phase 8.

</domain>

<decisions>
## Implementation Decisions

### Checagem de MX (EXTR-01)
- **D-01:** Usar a lib **DnsClient.NET** (NuGet) para query MX de verdade. Decidido contra
  shell-out para `nslookup` (o que o Python faz) porque `Process.Start` em shared hosting
  (SmarterASP/IIS) é frágil e pode estar bloqueado; e contra checar só A/AAAA via
  `System.Net.Dns` porque é sinal fraco demais (domínio com A pode não receber e-mail).
- **D-02:** **Falha/timeout de DNS não rejeita o lead.** Lead passa e é registrado como
  "MX não verificado". Racional: falha de infraestrutura não é prova de lead ruim — é diferente
  do filtro geográfico, onde o dado ausente é do próprio lead (lá, não-verificável = fora).
- **D-03:** **Cache persistente por domínio com TTL**, espelhando o `mx-cache.json` (30 dias) do
  Python. Evita ~1000 queries DNS/dia re-consultando os mesmos domínios. Forma exata do storage
  (tabela, arquivo, IMemoryCache + persistência) fica a critério do planner.

### Registro de rejeição (EXTR-02)
- **D-04:** **Agregado por rodada na entidade `CustomerImport` que já existe** (FileName, Type,
  Status, TotalRecords, SuccessCount, FailedCount, ErrorDetails — 366 linhas, 1.6 MB hoje):
  contadores por motivo (geo fora do alvo / e-mail lixo / sem MX / duplicado) por execução do
  import. Consultável por período via `CreatedAt`.
- **D-05:** **Decidido contra tabela de rejeição por lead.** Motivo: o banco está em 467/500 MB
  de quota (`audit_logs` sozinho = 279 MB) e ~900 leads/dia são rejeitados hoje — uma tabela
  por lead precisaria de política de purge desde o primeiro dia. Aceita-se perder o "qual e-mail
  foi rejeitado por quê" em troca de custo zero de storage.

### Persistência da classificação de site (EXTR-03)
- **D-06:** Coluna nova em `Customer` — `WebsiteKind` (Unknown / OwnSite / Directory).
- **D-07:** **A migration desta coluna sai JUNTO com a coluna `ExternalId` da Phase 8, numa
  migration única.** Racional: a Phase 8 vai precisar de migration de qualquer jeito, e há uma
  pendência aberta de ordenação de migrations (ver Canonical Refs / risco do v1.2) — melhor um
  ponto de coordenação do que dois. **Implicação para o planner:** criar a migration durante a
  Phase 7 contendo AMBAS as colunas; a Phase 8 apenas consome `ExternalId` na lógica de dedup.
- **D-08:** O critério de "diretório de terceiro" reaproveita a lista que já existe em
  `docs/email-marketing/site_check.py` (`DIRECTORY_HOSTS`: econodata, cliniguia, facebook,
  instagram, linkedin, google maps, apontador, guiamais, jusbrasil, doctoralia, ifood, wixsite,
  linktr.ee, etc.) — não reinventar a lista.

### Destino da ponte Python (critério 4 da fase)
- **D-09:** **Aposentar a ponte manual assim que o `ExtractorPullWorker` for validado em
  produção.** O worker vira fonte única de import; `import_extrator_bridge.py` permanece no repo
  como fallback de emergência, documentado como "não usar em rotina". Zero divergência por
  construção — em vez de manter duas implementações sincronizadas.
- **D-10:** ⚠️ **Dependência externa:** D-09 depende da verificação da primeira execução real do
  `ExtractorPullWorker` (agendada 12:00 BRT / 15:00 UTC, verificação em andamento no momento em
  que este contexto foi escrito). Se o worker não estiver importando de fato, a aposentadoria da
  ponte não pode acontecer nesta fase — mas as decisões D-01..D-08 seguem válidas
  independentemente.

### Claude's Discretion
- Forma exata do cache de MX (tabela vs arquivo vs IMemoryCache + persistência)
- Timeout e política de retry da query DNS
- Como os contadores por motivo são serializados no `CustomerImport` (colunas novas vs JSON em
  `ErrorDetails`)
- Paralelismo/batching das queries DNS durante o import

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

Este projeto não usa ADRs formais. As referências canônicas são as implementações Python que já
resolvem exatamente estes problemas em produção e cuja lógica está sendo portada para C#:

### Lógica a ser portada (comportamento de referência)
- `docs/email-marketing/mx_check.py` — checagem de MX/domínio: `is_junk_domain` (placeholders,
  `.local`, wixpress, sentry), `parse_nslookup_mx` (Null MX RFC 7505 = sem MX), `resolve_mx`
  (fallback para registro A quando não há MX), cache 30 dias
- `docs/email-marketing/site_check.py` — `classify_url` e `DIRECTORY_HOSTS`: a lista canônica de
  "diretório de terceiro" para EXTR-03
- `docs/email-marketing/waves_lib.py` §`in_target_geo` — critério geográfico já espelhado no C#
  (`IsOutsideTargetGeo`), referência de como a paridade Python↔C# foi feita antes

### Código .NET que será modificado
- `api-core/src/Diax.Application/Customers/ExtractorIntegrationService.cs` — loop de import com
  `IsLowQualityEmail` e `IsOutsideTargetGeo`; ponto de inserção do MX check e da classificação
- `api-core/src/Diax.Application/Customers/ExtractorPullOptions.cs` — config bound da seção
  `ExtractorPull` (Enabled, DailyHourUtc, MaxPages, BlockedDomains, BlockedTlds, AllowedStates,
  AllowedDdds)
- `api-core/src/Diax.Domain/Customers/CustomerImport.cs` — entidade de rodada de import onde os
  contadores de rejeição (D-04) vão morar

### Contexto do milestone e risco operacional
- `.planning/BACKLOG-v1.3-pipeline-aquisicao.md` — backlog de origem com a evidência de cada item
- `.planning/STATE.md` §"v1.2 — Pausado" — ⚠️ **ler antes de criar qualquer migration**: a
  migration `20260529134701_AddAgentFoundation` do v1.2 já está aplicada em produção mas os
  commits **não foram pushados**; ordenar migrations com cuidado (D-07)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CustomerImport` (entidade + tabela, 366 linhas / 1.6 MB): já registra uma rodada de import com
  contadores — home natural dos contadores de rejeição, sem tabela nova
- `ExtractorPullOptions`: já é o ponto de configuração de listas de bloqueio; MX check e
  classificação de site podem ganhar toggles/TTL ali sem inventar novo mecanismo
- `ExtractorLead` DTO já tem `Website`, `State`, `City` (state/city adicionados em 05/09)
- Python `mx_check.py` / `site_check.py`: lógica testada em produção, com testes offline
  (`docs/email-marketing/tests/`) que servem de tabela-verdade para os testes C#

### Established Patterns
- `Result<T>` (Success/Failure com `Error`) em toda a camada Application
- Filtros de qualidade são métodos privados no próprio `ExtractorIntegrationService`, chamados
  em sequência no loop por lead — MX e classificação seguem o mesmo formato
- Testes: xUnit + Moq, `CreateSut(ExtractorPullOptions?)` já parametriza options nos testes
- Build/test **sempre** com `-c Release` (Smart App Control bloqueia DLL de teste Debug)

### Integration Points
- `ExtractorIntegrationService.ImportLeadsAsync` — loop por lead: `MapToImportRow` →
  `IsLowQualityEmail` → `IsOutsideTargetGeo` → (**novo:** MX check, classificação) →
  `allLeads.Add(row)`
- `ExtractorPullWorker` (Infrastructure/Workers) — chama o service diariamente 15:00 UTC
- `CustomerImportService` — consome as linhas e cria os `Customer`

</code_context>

<specifics>
## Specific Ideas

- "Não reinventar a lista de diretórios" — `DIRECTORY_HOSTS` do `site_check.py` é a fonte
- A paridade Python↔C# já foi feita uma vez (filtro geográfico, 05/09) e serve de modelo:
  mesma semântica, testes espelhados dos dois lados
- Motivação concreta desta fase: bounce de 7,5% na semana de 03-04/09 antes do MX check entrar
  na ponte Python; e 3 leads suíços (`.ch`, "Globus Karriere") que passaram por um caminho de
  import e não pelo outro

</specifics>

<deferred>
## Deferred Ideas

- **Google Places API como fonte alternativa de leads** (EXTR-04) — já está em v2 no
  REQUIREMENTS.md, investigativo, não entra nesta fase
- **Backfill dos leads já no banco que falhariam nos novos filtros** — surgiu como possível área
  de discussão; é limpeza de dado existente, não qualidade de entrada. Candidato a fase própria
  ou a script pontual depois que os filtros estiverem rodando
- **Verificação SMTP real (handshake com o servidor)** — nível acima de MX; alto risco de
  bloqueio/reputação, fora de escopo

</deferred>

---

*Phase: 07-extra-o-qualidade-na-entrada*
*Context gathered: 2026-09-05*
