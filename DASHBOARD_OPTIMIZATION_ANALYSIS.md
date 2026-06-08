# DIAX CRM Dashboard — Otimização Estratégica
**Relatório de Análise e Recomendações**  
Data: 2026-06-08

---

## 📊 Sumário Executivo

Sua aplicação DIAX CRM possui **infraestrutura de dados robusta** (71 controllers, múltiplos módulos integrados), mas o **dashboard home atual é genérico e não prioriza seus objetivos de negócio**. 

**Situação atual:**
- ✅ Widgets desconexos (13 widgets, apenas 3 relevantes para negócio)
- ✅ Métrica de CRM é contagem pura (leads + customers) — sem visibilidade de conversão
- ✅ Finance mostra apenas mês atual — sem tendências
- ✅ Email marketing bem implementado, mas isolado
- ✅ **Lacuna crítica:** Nenhuma visão de **lead funnel**, **revenue vs. goal**, **source attribution**, **pipeline velocity**

**Seu objetivo principal:** "Ganhar clientes, leads e vender serviços"  
→ **Tradução em KPIs:** Lead generation → Qualification → Conversion → Revenue

---

## 🎯 Recomendação Principal: 5-7 Métricas Prioritárias

Substitua a home do dashboard por uma tela executiva focada em **3 áreas críticas:**

### **1️⃣ PIPELINE DE VENDAS (Lead Funnel)**
Visualize a progressão de leads até cliente em um único gráfico.

**Métrica:** `Lead → Contacted → Qualified → Negotiating → Customer`  
**Visual recomendado:** Waterfall ou sankey chart  
**Ação:** Clique em cada estágio para drills-down na lista de leads  
**Endpoint necessário:** `GET /api/v1/customers/funnel-analytics` (NÃO existe, precisa criar)

```
LEADS (120) → CONTACTED (85, 71%) → QUALIFIED (45, 53%) → NEGOTIATING (12, 27%) → CUSTOMER (8, 67%)
```

**Por quê?** Você não sabe onde estão seus gargalos. Qual etapa perde mais leads? Qual tem conversão lenta?

---

### **2️⃣ RECEITA vs. META**
Objetivo: Comparar performance financeira contra suas metas.

**Métricas:**
- Receita do mês atual vs. meta
- Tendência (últimos 3 meses)
- Variância ($$ e %)

**Visual recomendado:** Circular progress + trend sparkline  
**Endpoint necessário:** Melhorar `GET /api/v1/finance/summary` para incluir goal tracking  

```
Receita Atual: R$ 24.500
Meta Mensal:   R$ 30.000
Variância:     -R$ 5.500 (-18%)

Tendência: 📊 ↗ (crescendo desde mês passado)
```

**Por quê?** Finanças são prioridade. Você precisa de visão clara de performance vs. objetivo.

---

### **3️⃣ ATRIBUIÇÃO DE LEADS POR FONTE**
Entenda qual canal de prospecção gera leads de melhor qualidade.

**Métrica:** Leads por fonte (Manual, Scraping, Import, GoogleMaps) com taxa de conversão  
**Visual recomendado:** Pie chart com drill-down  
**Endpoint necessário:** `GET /api/v1/customers/by-source` (NÃO existe)

```
🔵 Google Maps:  32 leads, 25% conversão (melhor ROI)
🟢 Scraping:     45 leads, 12% conversão
🟡 Manual:       28 leads, 20% conversão
🔴 Import:       15 leads,  5% conversão (pior)
```

**Por quê?** Você investe em 4 canais, mas qual gera melhor cliente? Alocando recursos errado?

---

### **4️⃣ VELOCIDADE DO PIPELINE (Tempo em cada estágio)**
Qualquer gargalo causa atraso na venda.

**Métrica:** Tempo médio (dias) que lead passa em cada estágio  
**Visual recomendado:** Horizontal bar chart  
**Endpoint necessário:** `GET /api/v1/customers/funnel-analytics` (mesmo que #1)

```
Lead → Contacted:     3 dias (ótimo ⚡)
Contacted → Qualified: 8 dias (normal)
Qualified → Negotiating: 14 dias (lento ⚠️)
Negotiating → Customer: 7 dias (normal)
```

**Por quê?** "Negotiating" está demorando muito? Problema de qualificação anterior ou vendedor lento?

---

### **5️⃣ PERFORMANCE DE EMAIL MARKETING (7 últimos dias)**
Mantenha a visão de email, mas evolua.

**Métrica:** Campanhas enviadas, abertura, cliques (últimos 7 dias)  
**Visual recomendado:** Mini cards + sparkline  
**Endpoint:** Melhorar `/api/v1/email-campaigns/analytics?days=7`

```
Últimos 7 dias:
📧 Enviados: 1.250 | 📖 Abertos: 312 (25%) | 🔗 Cliques: 78 (6%)
```

**Por quê?** Email é motor de outreach. Acompanhe tendência semanal.

---

### **6️⃣ ALERTAS CRÍTICOS (Actionable Inbox)**
Surfaciar leads "em risco" automaticamente.

**Métrica:** Leads sem contato há 7+ dias, negociações paradas, cash flow negativo  
**Visual recomendado:** Card simples com badges  
**Endpoint necessário:** `GET /api/v1/dashboard/alerts` (NÃO existe)

```
⚠️ 5 leads sem contato há 7+ dias → Clique para contatar agora
⚠️ 2 negociações paradas há 14 dias → Mover para perdido ou reavaliar
⚠️ Cash flow negativo este mês: -R$ 2.300 → Revisar despesas
```

**Por quê?** Preciosa informação fica enterrada em outras páginas.

---

### **7️⃣ RESUMO FINANCEIRO — EXPANDIDO (3 meses)**
Tendência, não snapshot.

**Métrica:** Receita, despesas, fluxo de caixa (mês atual, mês passado, média 3 meses)  
**Visual recomendado:** 3 colunas com trend indicators  
**Endpoint:** Melhorar `/api/v1/finance/summary`

```
                Atual      Anterior   Média 3M
Receita:        R$ 24.5k   R$ 22.1k   R$ 21.8k ↗
Despesas:       R$ 18.2k   R$ 19.5k   R$ 19.1k ↘
Fluxo Caixa:    R$ 6.3k    R$ 2.6k    R$ 2.7k  ↗
```

**Por quê?** Ver trends, não apenas snapshot. É receita crescendo? Despesas controladas?

---

## 🏗️ Arquitetura Recomendada do Dashboard Home

### Layout Visual (Responsive Grid)

```
┌─────────────────────────────────────────────────────────┐
│ Dashboard                    Bem-vindo, {email} [{roles}] │
└─────────────────────────────────────────────────────────┘

[ROW 1 — SALES & REVENUE] (Critical Path)
┌──────────────────────────────────┬─────────────────────┐
│ LEAD FUNNEL (Waterfall)          │ REVENUE vs. META    │
│ Lead→Contacted→Qualified→...     │ R$ 24.5k / R$ 30k  │
│ Click stage → filter leads page  │ -18% variance ⚠️   │
└──────────────────────────────────┴─────────────────────┘

[ROW 2 — CHANNELS & PIPELINE]
┌──────────────┬──────────────┬──────────────┐
│ LEAD SOURCES │ PIPE VELOCITY│ EMAIL PERF   │
│ (Pie chart)  │ (Bar chart)  │ 7-day stats  │
│ Google: 25%  │ Stage times  │ 1.25k sent   │
│ Scraping: 45%│ (bottlenecks)│ 25% open     │
└──────────────┴──────────────┴──────────────┘

[ROW 3 — FINANCIAL HEALTH]
┌──────────────────────────────────────┬──────────────┐
│ FINANCIAL SUMMARY (3 months)         │ ALERTS       │
│ Receita ↗ | Despesas ↘ | Fluxo ↗    │ ⚠️ 5 leads   │
│ Last 3 months comparison             │    without   │
│                                      │    contact   │
└──────────────────────────────────────┴──────────────┘

[ROW 4 — OPTIONAL DEEP DIVES] (Collapsible or below fold)
┌──────────────┬──────────────┬──────────────┐
│ INVESTMENTS  │ FB ADS TOP 3 │ AGENDA       │
│ (InvestIQ)   │ (Top camps)  │ (Upcoming)   │
│ ROI: +12.3%  │ ROAS tracking│ This week    │
└──────────────┴──────────────┴──────────────┘
```

### Remoção de Widgets de Baixa Prioridade

Mover para página `/dashboard/personal` ou `/admin/household`:
- ❌ ShoppingListWidget (casa, não negócio)
- ❌ RecentSnippetsWidget (ferramentas dev)
- ❌ RecentPromptsWidget (AI internal)
- ❌ SystemHealthWidget (ops, não exec)

Keep:
- ✅ AiInsightsWidget (insights de negócio)
- ✅ UpcomingAgendaWidget (compromissos)

---

## 🔧 O Que Precisa Ser Construído

### Tier 1 — CRÍTICO (Semana 1)

**Backend Endpoints (3 novos + 2 melhorias):**

1. **POST/GET `/api/v1/customers/funnel-analytics`**
   ```csharp
   Response:
   {
     stages: [
       { stage: "Lead", count: 120, avgDaysInStage: 0, conversionRate: 0.71 },
       { stage: "Contacted", count: 85, avgDaysInStage: 3, conversionRate: 0.53 },
       ...
     ],
     totalLeads: 120,
     conversionRate: 0.067
   }
   ```
   Location: Create `FunnelAnalyticsService` in `Diax.Application/Customers/`

2. **GET `/api/v1/customers/by-source`**
   ```csharp
   Response:
   {
     sources: [
       { source: "GoogleMaps", count: 32, conversionRate: 0.25, avgLeadQuality: 4.2 },
       { source: "Scraping", count: 45, conversionRate: 0.12, avgLeadQuality: 2.8 },
       ...
     ]
   }
   ```

3. **GET `/api/v1/dashboard/alerts`**
   ```csharp
   Response:
   {
     alerts: [
       { type: "LeadNoContact", severity: "warning", count: 5, message: "5 leads without contact > 7 days" },
       { type: "NegotiationStalled", severity: "warning", count: 2, message: "2 negotiations stalled > 14 days" },
       { type: "NegativeCashFlow", severity: "error", amount: -2300, message: "Negative cash flow this month" },
       ...
     ]
   }
   ```

**Frontend Widgets (5 new + 2 refactored):**

1. `LeadFunnelWidget.tsx` — Waterfall chart (recharts or similar)
2. `LeadSourceAttributionWidget.tsx` — Pie chart with drill-down
3. `SalesPipelineVelocityWidget.tsx` — Horizontal bar (avg days per stage)
4. `CriticalAlertsWidget.tsx` — Minimal actionable list
5. `ExpandedFinanceSummaryWidget.tsx` — 3-month comparison
6. Refactor `FinanceSummaryWidget` — merge into #5
7. Refactor `EmailSummaryWidget` — add 7-day sparkline

**Arquivo de entrada:** `crm-web/src/app/dashboard/page.tsx`

---

### Tier 2 — IMPORTANTE (Semana 2-3)

**Backend Enhancements:**
- Enhance `/api/v1/finance/summary` to support `includeMonthHistory: true, months: 3`
- Create `GET /api/v1/email-campaigns/top-performers?limit=5&days=30`
- Integrate `GET /api/v1/investiq/dashboard-summary` (existing controller)

**Frontend Widgets:**
- `InvestmentPortfolioWidget.tsx` — ROI + allocation
- `FacebookAdsTopCampaignsWidget.tsx` — Top 3 campaigns
- `EmailSuppressionHealthWidget.tsx` — Bounce/unsub rates

---

### Tier 3 — NICE TO HAVE (Semana 4+)

- Date range picker for all metrics (today, last 7d, last 30d, custom)
- Drill-down interactivity (click funnel stage → pre-filtered `/leads` page)
- Export dashboard as PDF
- Custom widget layout (drag-and-drop)
- Performance optimization (cache analytics endpoints for 5 min)

---

## 🛠️ Como Usar Obra Superpowers para Estruturação

Sua CLAUDE.md menciona **Obra Superpowers** como ferramentas disponíveis:
- `/brainstorm` — Refine ideas via questions
- `/write-plan` — Break work into atomic 2-5 min tasks
- `/execute-plan` — Execute with fresh subagents

**Recomendação:**
1. Run `/write-plan` para estruturar implementação em sprints
2. Use `/execute-plan` para paralelizar desenvolvimento de widgets
3. Use `/gsd:new-project` se quiser máximo detalhe e rastreamento

---

## 📊 Comparação: ANTES vs. DEPOIS

### ANTES (Atual)
```
Dashboard genérico (13 widgets mistos)
- Leads: 120 | Customers: 28 (sem contexto)
- Fluxo: R$ 6.3k | Receitas: R$ 24.5k | Despesas: R$ 18.2k (apenas mês atual)
- Email: 1.2k enviados, 25% abertura (isolado)
- UI clutter: shopping list, snippets, prompts, system health (ruído)

Tempo para responder "Qual meu principal gargalo?"
→ Visitar 4 páginas diferentes, nenhuma responde direto
```

### DEPOIS (Recomendado)
```
Dashboard focado em negócio (7 métricas críticas)
- LEAD FUNNEL → Vejo exatamente onde perco leads (ex: 40 em "Qualified" mas só 12 em "Negotiating")
- REVENUE vs. META → -18% de variância, clara oportunidade
- LEAD SOURCES → GoogleMaps 25% conversão vs. Scraping 12% → Alocar mais budget para GoogleMaps
- PIPE VELOCITY → "Negotiating" estágio demora 14 dias (bottleneck)
- EMAIL TREND → 25% abertura, parece estável
- ALERTS → 5 leads sem contato, 2 negociações paradas (ação imediata)
- FINANCIAL TREND → Receita crescendo 3 meses seguidos

Tempo para responder "Qual meu principal gargalo?"
→ Olho para dashboard, identifica em <10 segundos
```

---

## ✅ Validação de Dados

Antes de implementar, validar:

- [ ] `Customer.Status` enum mapping correto (0=Lead, 1=Contacted, 2=Qualified, 3=Negotiating, 4=Customer)
- [ ] Todos os leads têm `CreatedAt` preenchido (para cálculos de velocity)
- [ ] Email campaigns têm webhook tracking habilitado (opens/clicks no DB)
- [ ] Financial goals estão preenchidas com target amounts
- [ ] Lead sources (GoogleMaps, Scraping, Manual, Import) estão consistentes
- [ ] InvestIQ endpoint acessível e retorna dados válidos

---

## 📋 Próximos Passos Recomendados

**Opção A — Rápida (3-5 dias):**
1. Implementar endpoints Tier 1
2. Refatorar dashboard com 5 widgets principais
3. Deploy e validar com dados reais

**Opção B — Completa (10-14 dias):**
1. Executar Opção A
2. Adicionar Tier 2 widgets (investimentos, ads)
3. Implementar date range picker + drill-downs
4. Performance tuning

**Opção C — Com Orquestração (Recomendada):**
1. Rodar `/write-plan` neste documento → divide em sprints
2. Rodar `/execute-plan` para paralelizar desenvolvimento
3. Entrega incremental (semana 1 = 5 metrics, semana 2 = 2 more, semana 3 = polish)

---

## 🎯 Métricas de Sucesso

Quando dashboard estiver pronto, medir:

- **Load time:** < 2 segundos (endpoints otimizados + cache)
- **Acurácia:** Lead funnel = leads page total count (100% match)
- **Frescura:** Email metrics atualizadas em < 5 min
- **Adoção:** Usuário visualiza dashboard diariamente (ao invés de navegar 4 páginas)
- **Conversão:** Insights do dashboard levam a ações (ex: "GoogleMaps está 2x melhor, aumento budget")

---

## 📚 Referências Internas

- **CLAUDE.md:** Instruções globais e modular
- **Dashboard code:** `crm-web/src/app/dashboard/page.tsx`
- **Widgets:** `crm-web/src/components/dashboard/`
- **Finance service:** `crm-web/src/services/finance.ts`, `customers.ts`, `leads.ts`
- **Backend architecture:** `api-core/src/Diax.Application/`, `Diax.Domain/`
- **Database:** SmarterASP `sql1002.site4now.net`, `db_aaf0a8_diaxcrm`
- **Swagger:** http://localhost:5062/swagger (development only)

---

## 💡 Conclusão

Você tem **excelente infraestrutura de dados e integração**, mas o dashboard home não reflete seus **objetivos críticos de negócio** (lead generation, conversão, revenue).

**Recomendação final:**
Investir **2-3 semanas** para transformar dashboard em **ferramenta executiva de decisão**, focado nos 7 KPIs críticos que dirão você "o quê fazer agora" — sem necessidade de navegar entre 5 páginas diferentes.

O retorno é **imediato:** Melhor visibilidade = melhor alocação de recursos = mais clientes/receita.

---

**Próximo passo:** Confirmar priorização dos 7 KPIs e iniciar implementação (Tier 1) com `/write-plan` + `/execute-plan`.

