# DIAX CRM — Data Mapping for Premium Dashboard

## Available Data Sources (71 Controllers)

### 1. CRM FUNNEL (Customers + Leads)
**Endpoint:** `/api/v1/customers`, `/api/v1/leads`
**Available:**
- `Customer.Status` (0=Lead, 1=Contacted, 2=Qualified, 3=Negotiating, 4=Customer)
- `Customer.Segment` (Cold, Warm, Hot)
- `Customer.Source` (Manual, Scraping, Import, GoogleMaps)
- `Customer.CreatedAt`, `Customer.UpdatedAt` (timing)
- `Customer.Email`, `Customer.Phone`, `Customer.Company`

**Can Build (no new endpoint needed):**
- ✅ Lead Funnel visualization (group by status)
- ✅ Lead Source Attribution (pie chart by source)
- ✅ Segment Distribution (bar chart)
- ✅ Conversion Rate by Source (calculated)
- ✅ Pipeline Velocity (avg days between status changes — requires activity log)
- ✅ Recent Lead Activity (latest updates)

### 2. FINANCE (Income, Expense, Transactions)
**Endpoints:** `/api/v1/finance/summary`, `/api/v1/transactions`, `/api/v1/credit-cards`, `/api/v1/financial-goals`
**Available:**
- `Income.Amount`, `Income.Date`, `Income.Category`, `Income.Description`
- `Expense.Amount`, `Expense.Date`, `Expense.Category`, `Expense.Description`
- `Transaction.Type`, `Transaction.Amount`, `Transaction.Date`, `Transaction.Category`
- `FinancialGoal.TargetAmount`, `FinancialGoal.CurrentAmount`, `FinancialGoal.DueDate`
- `CreditCard.Balance`, `CreditCard.Limit`, `CreditCard.DueDate`
- `MonthlySimulation` data (projections)

**Can Build:**
- ✅ Revenue vs. Goal (progress bar + sparkline)
- ✅ Monthly Revenue Trend (area chart, last 6 months)
- ✅ Expense Breakdown (stacked bar by category)
- ✅ Cash Flow Forecast (line chart using simulations)
- ✅ Credit Card Health (gauge by utilization %)
- ✅ Recurring Income/Expense (bar chart)
- ✅ Goal Progress (progress ring)

### 3. EMAIL MARKETING
**Endpoints:** `/api/v1/email-campaigns`, `/api/v1/email-campaigns/analytics?days=30`
**Available:**
- `EmailCampaign` metrics: SentCount, OpenedCount, ClickedCount, CreatedAt, SentAt
- Computed: OpenRate, ClickRate
- Rolling window: 7, 14, 30 days

**Can Build:**
- ✅ Campaign Performance Table (sortable, filterable)
- ✅ Email Performance Trend (line chart, last 30 days)
- ✅ Open Rate vs. Click Rate (scatter/bubble chart)
- ✅ Top Performing Campaign (cards)
- ✅ Weekly Email Volume (bar chart)

### 4. ADS (Facebook)
**Endpoint:** `/api/v1/ads` (AdsController)
**Available:**
- Campaign metrics: Spend, Impressions, Clicks, Conversions, ROAS
- Daily breakdown data

**Can Build:**
- ✅ Top 3 Campaigns by ROAS (cards)
- ✅ Ad Spend Trend (area chart)
- ✅ ROI per Campaign (bar chart, sortable)
- ✅ Impression Volume (mini chart)

### 5. GOALS & FORECASTS
**Endpoints:** `/api/v1/financial-goals`, `/api/v1/monthly-simulations`
**Available:**
- Goal metrics: Name, TargetAmount, CurrentAmount, Progress%
- Simulation: MonthYear, ProjectedIncome, ProjectedExpense

**Can Build:**
- ✅ Goal Progress Rings (multiple)
- ✅ 3-Month Forecast (area chart: actuals + projection)
- ✅ Goal Completion Rate (percentage badges)

### 6. AGENDA (Appointments)
**Endpoint:** `/api/v1/appointments`
**Available:**
- Appointment.Title, StartTime, EndTime, Type

**Can Build:**
- ✅ Upcoming Events Mini Calendar (next 7 days)

### 7. INVESTMENTS (InvestIQ)
**Endpoint:** `/api/v1/investiq` (InvestIQController)
**Available:**
- Portfolio holdings, performance, ROI, asset allocation

**Can Build:**
- ✅ Portfolio ROI (single metric)

### 8. AI USAGE & COSTS
**Endpoints:** `/api/v1/ai/usage-logs` (AiUsageLogsController)
**Available:**
- AiUsageLog: Model, Cost, TokensUsed, CreatedAt

**Can Build:**
- ✅ AI Cost Trend (area chart)
- ✅ Model Usage Distribution (horizontal bar)

---

## 10 PREMIUM CHARTS — BUILD TODAY (No new endpoints)

| # | Chart | Type | Data Source | Complexity | Impact |
|---|-------|------|-------------|-----------|--------|
| 1 | **Revenue vs. Goal** | Circular progress + sparkline | Finance | Low | 🔴 CRITICAL |
| 2 | **Lead Funnel** | Waterfall/Sankey | Customers | Low | 🔴 CRITICAL |
| 3 | **Revenue Trend** | Animated area chart | Transactions | Medium | 🔴 CRITICAL |
| 4 | **Lead Source** | Donut with drill-down | Customers | Low | 🟠 HIGH |
| 5 | **Email Campaigns** | Animated table | EmailCampaigns | Medium | 🟠 HIGH |
| 6 | **Expense Breakdown** | Stacked horiz. bar | Expenses | Low | 🟠 HIGH |
| 7 | **Cash Flow Forecast** | Dual-line chart | Trans + Simulations | High | 🟠 HIGH |
| 8 | **Top Ads** | Cards + micro-bars | Facebook Ads | Medium | 🟡 MEDIUM |
| 9 | **Goal Rings** | Radial progress | FinancialGoals | Low | 🟡 MEDIUM |
| 10 | **Lead Segments** | Stacked bar | Customers | Low | 🟢 NICE |

---

## HERO DASHBOARD WIREFRAME

```
┌─────────────────────────────────────────────────────┐
│ DIAX Dashboard          📊 Jun 8 | Welcome, Alexandre │
└─────────────────────────────────────────────────────┘

[HERO ROW — 4 Key Metrics]
┌────────────┬────────────┬────────────┬────────────┐
│ REVENUE    │ GROWTH     │ GOAL %     │ FORECAST   │
│ R$ 24.5k   │ +12.3%     │ 82%████    │ R$ 28.2k   │
│ -18% vs    │ MoM        │ (6 goals)  │ next 45d   │
│ goal ⚠️    │ ↗ trend    │            │ +16% proj  │
└────────────┴────────────┴────────────┴────────────┘

[ROW 2 — SALES PIPELINE + SOURCES]
┌──────────────────────┬─────────────────┐
│ LEAD FUNNEL          │ SOURCE SPLIT    │
│ (Waterfall)          │ (Donut)         │
│ 120→85→45→12→8       │ GoogleMaps: 28% │
│ Conv: 6.7% | Drop %  │ Scraping: 42%   │
└──────────────────────┴─────────────────┘

[ROW 3 — REVENUE + EMAIL]
┌──────────────────────┬─────────────────┐
│ REVENUE TREND        │ EMAIL PERF      │
│ (Area, 6 months)     │ (3-day KPIs)    │
│ ↗ Clear upward       │ 1.2k sent       │
│ Hover for details    │ 25% open ✓      │
└──────────────────────┴─────────────────┘

[ROW 4 — EXPENSES + ALERTS]
┌──────────────────────┬─────────────────┐
│ EXPENSES             │ ACTION ITEMS    │
│ (Stacked bar)        │ ⚠️ 5 leads     │
│ Salaries 45%         │   no contact    │
│ Tools 20%, etc.      │ ✅ +12% revenue │
└──────────────────────┴─────────────────┘

[ROW 5 — SECONDARY (Optional/Collapsible)]
┌──────────────┬──────────────┬──────────────┐
│ FORECAST     │ TOP ADS      │ GOALS        │
│ 3-month cash │ Top 3 ROAS   │ Progress     │
│ (Line chart) │ (Cards)      │ (Rings)      │
└──────────────┴──────────────┴──────────────┘
```

