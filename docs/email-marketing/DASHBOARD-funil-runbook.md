# Dashboard de Conversão do Funil — Runbook (DASH-01)

Gerado em: 2026-09-20 (Phase 14, plano 14-01)

Escopo travado por `14-CONTEXT.md` (D-01): **verificar e documentar**, não construir. O dashboard
comercial já existe em produção desde 2026-07-03 (PR #61, commit `c200f4b7`) — mais de dois meses
antes de este milestone (v1.4) começar. Este runbook prova ao vivo que a fórmula do funil está
correta, mostra prova visual de que a página renderiza, e registra os dois achados genuínos da
pesquisa: a natureza cumulativa de `conversionToNext` e a ausência do filtro por período. Nenhuma
linha foi alterada em `SalesDashboardService.cs`, `SalesDashboardController.cs`, `page.tsx` ou
`salesDashboard.ts` para produzir este documento (`git diff --stat` confirmado vazio nas três tasks
deste plano).

Esta é a **última fase do milestone v1.4** — ver seção 10 para o fechamento do milestone inteiro.

---

## 1. O que existe hoje

| Camada | Arquivo | Papel |
|---|---|---|
| Backend (fórmula) | `api-core/src/Diax.Application/Customers/SalesDashboardService.cs` | `BuildFunnel` calcula contagem + `conversionToNext` por etapa |
| Backend (endpoint) | `api-core/src/Diax.Api/Controllers/V1/SalesDashboardController.cs` | `GET /api/v1/sales-dashboard`, `[Authorize]` |
| Frontend (serviço) | `crm-web/src/services/salesDashboard.ts` | `getSalesDashboard()` → `apiFetch('/sales-dashboard')` — respeita a regra do `CLAUDE.md` de não fazer `fetch` direto em componente |
| Frontend (página) | `crm-web/src/app/dashboard-comercial/page.tsx` | Renderiza KPIs, o funil (`Funil de vendas`), receita mensal e agenda comercial |

Entregue no **PR #61**, commit `c200f4b7`, **2026-07-03** — mais de dois meses antes do v1.4
começar. A página está linkada no menu principal (`crm-web/src/components/AppShell.tsx`, seção
"CRM" → "Dashboard Comercial") e no command palette (`Ctrl+K` → "Ir para Dashboard Comercial"): não
é página órfã, confirmado por `grep` sem nenhuma outra referência concorrente a
`/dashboard-comercial`.

## 2. A fórmula, verbatim

Fonte: `api-core/src/Diax.Application/Customers/SalesDashboardService.cs`, linhas 94-116:

```csharp
private static readonly (CustomerStatus Status, string Label)[] FunnelStages =
{
    (CustomerStatus.Lead, "Leads"),
    (CustomerStatus.Contacted, "Contactados"),
    (CustomerStatus.Qualified, "Qualificados"),
    (CustomerStatus.Negotiating, "Negociando"),
    (CustomerStatus.Customer, "Clientes"),
};

public static List<FunnelStageDto> BuildFunnel(Dictionary<CustomerStatus, int> counts)
{
    var result = new List<FunnelStageDto>();
    for (var i = 0; i < FunnelStages.Length; i++)
    {
        var (status, label) = FunnelStages[i];
        var count = counts.GetValueOrDefault(status);

        decimal? conversion = null;
        if (i < FunnelStages.Length - 1)
        {
            var ahead = FunnelStages.Skip(i + 1).Sum(s => counts.GetValueOrDefault(s.Status));
            var basis = count + ahead;
            conversion = basis > 0 ? Math.Round((decimal)ahead / basis, 4) : null;
        }

        result.Add(new FunnelStageDto(status.ToString(), label, count, conversion));
    }
    return result;
}
```

Por que é confiável: `BuildFunnel` é uma função `static` **pura** — um `Dictionary<CustomerStatus,
int>` entra, uma `List<FunnelStageDto>` sai, sem EF Core, sem DI, sem I/O. Mesmo padrão de
`decide()`/`veredito()` usado nos scripts Python das Fases 12/13 para permitir teste 100%
determinístico sem infraestrutura.

## 3. ⚠️ `conversionToNext` é razão CUMULATIVA, não taxa adjacente

Este é o achado central desta fase. `ahead` soma **TODAS** as etapas posteriores à atual — não só a
próxima etapa. A fórmula é:

```
conversionToNext[etapa] = (soma de TODOS além desta etapa) / (esta etapa + soma de TODOS além dela)
```

Ou seja: "qual fração do pool que está nesta etapa ou além dela já passou desta etapa" — uma métrica
legítima de saúde de funil, mas **não** é o que o rótulo da UI (`↓ X% avançaram`,
`crm-web/src/app/dashboard-comercial/page.tsx`, linha 142) sugere. O texto do ROADMAP.md
("Contacted→Qualified") também sugere leitura adjacente estrita — errada.

**Não leia** o número sob "Contactados" como "X% dos contactados viraram Qualificados". A conta real
com os números medidos ao vivo nesta execução (seção 6):

- Etapa **Contactados**: `count = 2972`. `ahead` = soma de Qualificados(34) + Negociando(0) +
  Clientes(2) = **36**. `basis = 2972 + 36 = 3008`. `conversion = 36 / 3008 = 0.01196 ≈ 0.012` →
  tela mostra "↓ 1.2% avançaram" — **não** é a taxa Contactados→Qualificados isolada (que seria
  34/3008 se fosse adjacente); é a fração de todo o pool "Contactados-ou-além" que já está em
  Qualificados, Negociando ou Cliente.

## 4. ⚠️ A ilusão dos 100% em etapa vazia

Com os números medidos nesta execução: `Negociando.count = 0`. `ahead` = soma de Clientes(2) = `2`.
`basis = 0 + 2 = 2`. `conversion = 2 / 2 = 1.0` → a tela mostra **"↓ 100.0% avançaram"** embaixo de
uma barra com **zero** leads visíveis (confirmado no screenshot da seção 7).

Matematicamente correto — o guard `basis > 0` faz exatamente o que deveria (evita `NaN`/exceção
quando `count == 0`) — mas visualmente enganoso: um leitor apressado pode interpretar "100%
avançaram" como um sinal positivo forte, quando na verdade a etapa está vazia e o número só reflete
que os 2 Clientes que existem "avançaram" além de uma etapa que não tinha ninguém.

Marcado como **quirk conhecido e aceito**, não bug aberto — D-01 barra correção de código nesta
fase. Se um dia for corrigido, a correção seria suprimir ou ressalvar visualmente o percentual
quando `count == 0` (ex.: não exibir a linha "↓ X% avançaram" nesse caso específico, já que a
proporção com denominador dominado por etapas futuras não tem leitura útil quando a própria etapa
está vazia).

## 5. Escopo do funil: 5 de 7 status

`CustomerStatus` (`api-core/src/Diax.Domain/Customers/Enums/CustomerStatus.cs`) tem **7** valores:
`Lead=0, Contacted=1, Qualified=2, Negotiating=3, Customer=4, Inactive=5, Churned=6`. `BuildFunnel`
cobre só os 5 primeiros — `Inactive` e `Churned` existem no dicionário bruto retornado por
`GetStatusCountsAsync` (um `GroupBy(c => c.Status)` sem exclusão, em
`CustomerRepository.cs:77-83`), mas nunca são lidos pelo laço de `BuildFunnel`, que itera só
`FunnelStages`.

Fronteira **deliberada**, não bug: o funil mostra o pipeline ativo (em progresso + ganho), não
atrito. Vale registrar explicitamente porque o `14-CONTEXT.md` cita `Inactive = 1065` como número de
referência da sessão de auditoria — um leitor poderia esperar ver esse número refletido em algum
lugar desta tela, e ele corretamente não aparece.

## 6. Números verificados ao vivo

Captura reproduzida nesta execução (não copiada do `14-RESEARCH.md`), gravada em
`docs/email-marketing/previews/dashboard-funil-evidencia.json`:

- **Capturado em:** `2026-09-20T20:02:07-03:00` (BRT) / `2026-09-20T23:02:07Z` (UTC)
- **Fonte:** `POST https://api.alexandrequeiroz.com.br/api/v1/auth/login` (obtém `accessToken`) →
  `GET https://api.alexandrequeiroz.com.br/api/v1/sales-dashboard` (header `Authorization: Bearer`)

| Etapa | count | ahead | basis | esperado | retornado | bate |
|---|---|---|---|---|---|---|
| Lead | 4246 | 3008 | 7254 | 0.4147 | 0.4147 | ✅ |
| Contacted | 2972 | 36 | 3008 | 0.012 | 0.012 | ✅ |
| Qualified | 34 | 2 | 36 | 0.0556 | 0.0556 | ✅ |
| Negotiating | 0 | 2 | 2 | 1.0 | 1 | ✅ |
| Customer | 2 | — | — | null | null | ✅ |

**Veredito: `BATE`** — todo `conversionToNext` servido em produção confere com a fórmula do código,
zero discrepância encontrada, dentro da tolerância `1e-4`.

`delta_vs_pesquisa` (comparação contra o snapshot do `14-RESEARCH.md`, também de 2026-09-20): Lead
4246→4246 (delta 0), Contacted 2972→2972 (delta 0), Qualified 34→34 (delta 0), Negotiating 0→0
(delta 0), Customer 2→2 (delta 0). Deriva zero nesta janela específica — a pesquisa e a re-medição
desta execução caíram no mesmo dia sem nenhum lead novo transicionando de etapa entre as duas
capturas. Isso não significa que os números sejam estáticos: são um snapshot, e vão derivar
naturalmente com o outreach ativo das Fases 12/13 (ver Nota de manutenção, seção 9).

### Como reproduzir

```
POST https://api.alexandrequeiroz.com.br/api/v1/auth/login
  body: {"email": "<DIAX_ADMIN_EMAIL>", "password": "<DIAX_ADMIN_PASSWORD>"}
  → { "accessToken": "..." }

GET https://api.alexandrequeiroz.com.br/api/v1/sales-dashboard
  header: Authorization: Bearer <accessToken>
  → { "funnel": [{ "stage", "label", "count", "conversionToNext" }, ...], ... }
```

Credenciais em `C:/Users/acq20/.claude/.secrets.env` (`DIAX_ADMIN_EMAIL`/`DIAX_ADMIN_PASSWORD`).
Re-derivar `ahead`/`basis`/`esperado` manualmente por etapa como na seção 2, comparar com
`conversionToNext` retornado, tolerância `1e-4`.

## 7. Prova visual

`docs/email-marketing/previews/dashboard-funil-screenshot.png` (114,6 KB, PNG de página real,
capturado 2026-09-20T20:05:18-03:00) — screenshot `fullPage` da página
`/dashboard-comercial/` de produção, logada com admin real, mostrando o bloco "Funil de vendas" com
as 5 etapas (Leads 4.246, Contactados 2.972, Qualificados 34, Negociando 0, Clientes 2), as barras
proporcionais e os rótulos "↓ X% avançaram" — prova de que a página **renderiza de verdade** (D-02),
não só de que o endpoint responde.

Report da captura: `docs/email-marketing/qa-e2e/dashboard-funil-shot-report.json` — os 4 checks de
DOM (`titulo_funil`, `rotulo_conversao`, `cinco_etapas`, `sem_erro`) todos `true`, `ok: true`.

**`comparacao_api`** (números exibidos na tela vs. `funnel_bruto` da evidência da seção 6):
`bate_exato: true` — os 5 valores exibidos batem exatamente com os 5 valores da API, `delta: 0` em
todas as etapas. A evidência da API foi capturada em `2026-09-20T20:02:07-03:00`, o screenshot em
`2026-09-20T20:05:18-03:00` — 3 minutos e 11 segundos de intervalo, sem nenhuma transição de etapa
nesse meio-tempo (mesmo padrão de leitura estável da seção 6).

**Como regerar:**

```
cd docs/email-marketing/qa-e2e
node dashboard-funil-shot.mjs
```

Modelado em `docs/email-marketing/qa-e2e/crm-e2e.mjs` (mesmo login real, contexto Chrome desktop
`pt-BR`/`America/Sao_Paulo`, stealth `navigator.webdriver`).

## 8. Cobertura automatizada existente

`api-core/tests/Diax.Tests/Application/Customers/SalesDashboardTests.cs` já cobre `BuildFunnel`
antes desta fase começar:

- `BuildFunnel_ComputesConversionToNext` — verifica o cálculo da conversão numa amostra fixa
  (`Lead=90, Contacted=5, Qualified=3, Negotiating=1, Customer=1` → 10% na primeira etapa)
- `BuildFunnel_EmptyCounts_AllZeroNullConversion` — já exercita o guard de divisão por zero
  (`Dictionary` vazio → todas as contagens zero, `conversionToNext` nulo nas 4 primeiras etapas)

Comando (sempre `-c Release` — Smart App Control bloqueia DLL de teste Debug nesta máquina):

```
cd api-core
dotnet test --filter "FullyQualifiedName~SalesDashboardTests" -c Release
```

Rodado nesta execução: **5 testes, 0 falhas** (a suíte inclui `SalesDashboardTests` mais os demais
testes de `SalesDashboardService` já existentes no arquivo).

**Nenhum teste novo foi criado nesta fase, e por quê:** a fórmula já estava coberta, incluindo o
caso de divisão por zero. Criar um teste redundante para provar exatamente o que
`BuildFunnel_ComputesConversionToNext`/`BuildFunnel_EmptyCounts_AllZeroNullConversion` já provam
seria código novo sem valor incremental — proibido por D-01 (verify-only, zero código de aplicação
novo além do harness de captura de evidência).

## 9. Limites, gap deferido e validade

### GAP CONHECIDO E DEFERIDO — filtro por período

O critério de sucesso #3 do ROADMAP.md §Phase 14 ("Usuário pode filtrar o funil por período, ex.
última semana, último mês") **não existe em lugar nenhum do código**:

- `SalesDashboardController.Get()` não recebe nenhum parâmetro de data — só `CancellationToken ct`
- `crm-web/src/app/dashboard-comercial/page.tsx` não tem nenhum date picker ou seletor de intervalo
- O único gráfico com dimensão temporal (receita de propostas pagas) tem lookback fixo de 6 meses,
  não ajustável pelo usuário

Não foi construído nesta fase porque D-01 trava o escopo em verificar+documentar, não em adicionar
funcionalidade nova. **Este gap é o motivo de o critério #3 do ROADMAP não poder ser lido como
atendido** — DASH-01 fecha com essa ressalva explícita na rastreabilidade (seção 10 e
REQUIREMENTS.md).

O que seria necessário para fechar esse gap numa fase futura:
1. `SalesDashboardController.Get()` ganhar parâmetros opcionais `from`/`to` (ou um enum de período
   pré-definido: 7d/30d/90d/tudo)
2. `SalesDashboardService`/`GetStatusCountsAsync` filtrar a contagem de `Customer` por
   `CreatedAt`/`UpdatedAt` dentro do intervalo recebido
3. `page.tsx` ganhar um controle de período (dropdown ou date-range picker) que repasse o parâmetro
   para `getSalesDashboard()`

### Fora de escopo por decisão

Somar métricas de email/WhatsApp (Fases 12/13) nesta mesma tela do dashboard comercial — Deferred
Idea explícita do `14-CONTEXT.md`. Não avaliado, não planejado nesta fase.

### Validade

Os **números** (seção 6) são um snapshot datado e vão derivar diariamente com o outreach ativo das
Fases 12/13 — não confiar neles além de ~1-2 dias sem re-capturar. Os **achados de código** (fórmula,
navegação, deploy, testes, ausência do filtro por período) são estáveis e não precisam
re-verificação a menos que o código mude. Nota BRT vs UTC: todos os timestamps deste runbook estão
em `America/Sao_Paulo` (UTC-3) salvo indicação contrária — somar 3 horas antes de cruzar com logs em
UTC (ex. audit logs do CRM).

## 10. Fechamento do milestone v1.4

Esta fase (Phase 14) é a **última do milestone v1.4** ("Funil de Outreach — Cadência e Qualidade").
Com DASH-01 fechado (veredito `BATE` na re-derivação da fórmula + `ok: true` na prova visual — ver
seções 6 e 7), as **6 fases do v1.4 estão concluídas** (Phases 9, 10, 11, 12, 13, 14). Ver
`.planning/ROADMAP.md` §v1.4 e `14-01-SUMMARY.md` para o detalhamento completo do fechamento,
incluindo o estado honesto dos 14 requisitos (11 `Complete`, 3 `Partial` — EMAL-01/02/04, por decisão
deliberada de segurança de reputação/calendário, não por falha de implementação).

---

## Nota de manutenção

Esta evidência (seções 6 e 7) é um snapshot de 2026-09-20. Quem revalidar este runbook no futuro deve
re-rodar as duas chamadas HTTP da seção 6 **e** `node dashboard-funil-shot.mjs` (seção 7) contra o
estado real de produção do dia — não basta reler os números já registrados aqui, o funil muda todo
dia com o outreach ativo. Os achados de código (fórmula cumulativa, escopo 5-de-7 status, ausência do
filtro por período) permanecem válidos até que o código realmente mude — nesse caso, este runbook
fica desatualizado e precisa ser revisado, não só os números.
