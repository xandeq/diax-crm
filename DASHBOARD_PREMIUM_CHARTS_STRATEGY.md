# DIAX CRM — Premium Dashboard Charts Strategy

**Status:** Strategic Plan (Ready for Implementation)  
**Author:** Impeccable Design System  
**Date:** 2026-06-08

---

## 🎯 Executive Summary

Transform DIAX CRM dashboard into a **SaaS-grade visual intelligence engine** using **10 premium charts** built from existing data (71 controllers, zero new endpoints). Focus: revenue clarity, lead pipeline visibility, action-oriented design.

**Key Principles:**
- Every chart answers a business question
- Animations reveal insights, not distract
- One-click drill-downs to detailed pages
- Design inspired by Stripe, HubSpot, Vercel, Linear

---

## 🏆 THE 10 PREMIUM CHARTS (By Impact)

### 1. REVENUE VS. GOAL — Hero Metric
**Objective:** Instant clarity on revenue health  
**Business Value:** Know if you're on track in <2 seconds  
**Data Source:** `/api/v1/finance/summary` (existing)  
**Chart Type:** Circular progress + dual sparklines  
**Complexity:** Low (1-2 hours)  
**ROI Score:** 🔴 CRITICAL (10/10)

**Wireframe:**
```
┌────────────────────────────────────────┐
│ REVENUE THIS MONTH                     │
├────────────────────────────────────────┤
│                                        │
│         ╭─────────────╮                │
│        ╱      24.5k    ╲               │
│       │  ███████░░░░   │← 82%          │
│        ╲       Goal    ╱               │
│         ╰─────────────╯                │
│         R$ 30.000 target               │
│                                        │
│  ↗ +3.2% from last month               │
│  ⚠️ -18% from goal                     │
│                                        │
│  [Trend sparkline: ↗️↗️↗️↗️↗️ last 6m] │
│                                        │
│  💡 Needs R$ 5.5k more to hit goal    │
└────────────────────────────────────────┘
```

**Design Details:**
- Circular progress: Teal (primary color)
- Subtitle: Conditional color (green if on target, orange if <90%, red if <80%)
- Sparkline: Micro-animation (values reveal on scroll)
- Copy: Actionable insight below
- Hover state: Show daily breakdown in tooltip

**Interactions:**
- Click → Drill to `/finance` detail page
- Hover on sparkline → Show daily values
- Goal icon → Edit goal (modal or inline)

---

### 2. LEAD FUNNEL — Sales Pipeline Clarity
**Objective:** See exactly where leads get stuck  
**Business Value:** Identify bottleneck stages, optimize conversion  
**Data Source:** `/api/v1/customers` (group by status)  
**Chart Type:** Waterfall (Sankey alternative)  
**Complexity:** Medium (2-3 hours)  
**ROI Score:** 🔴 CRITICAL (10/10)

**Wireframe:**
```
LEAD FUNNEL — Conversion Path

Lead (120)
  │╲
  │ ╲→ Lost (-29%) = 35 leads
  │
  ├→ Contacted (85, 71%)
     │╲
     │ ╲→ Disqualified (-40%) = 34 leads
     │
     ├→ Qualified (45, 53%)
        │╲
        │ ╲→ Paused (-27%) = 12 leads
        │
        ├→ Negotiating (12, 27%)
           │╲
           │ ╲→ Lost (-33%) = 4 leads
           │
           └→ Customer (8, 67% final conv)

Legend:
━━━━━ Progression (teal)
╱╱╱╱╱ Drop-off (gray)

Metrics:
→ Stage times: Avg 8 days in Qualified (bottleneck!)
→ Best conversion: Contact→Qualified (53%)
→ Weakest: Negotiating→Customer (67%)
```

**Design Details:**
- Waterfall bars: Teal (progress), gray (drop-off)
- Labels: Count + % of previous stage
- Highlighted: Biggest drop-off stage
- Color intensity: Darker = worse conversion rate
- Animation: Bars fill top-to-bottom (reveal flow)

**Interactions:**
- Click any stage → Filter leads page to show only that stage
- Hover on drop-off → Show reasons (if data available, else prompt to add notes)
- Toggle: Show drop-off % vs. absolute numbers
- Drill: View individual leads in stage

---

### 3. MONTHLY REVENUE TREND — Historical Performance
**Objective:** Spot revenue trends and seasonality  
**Business Value:** Forecast next month with confidence  
**Data Source:** `/api/v1/transactions` (group by month)  
**Chart Type:** Animated area chart with dual-axis (revenue + target)  
**Complexity:** Medium (2-3 hours)  
**ROI Score:** 🔴 CRITICAL (9/10)

**Wireframe:**
```
REVENUE TREND — Last 6 Months

    R$ 30k ┤                           ╱─╲
           │                      ╱───╱   ╲
    R$ 25k ┤                 ╱───╱        ╲╲
           │            ╱───╱              ╲╲── Actual
    R$ 20k ┤      ╱────╱ 
           │  ╱──╱
    R$ 15k ┤═════════════════════════════════ Target (R$ 25k)
           │
           └─────────────────────────────────
           Dec  Jan  Feb  Mar  Apr  May  Jun

Actual:     21.2k 19.8k 22.5k 23.1k 24.5k
Target:     25k   25k   25k   25k   25k
Variance:   -15%  -21%  -10%  -8%   -2%

Insight: ↗ Trending toward target (only -2% now!)
```

**Design Details:**
- Area fill: Teal (actual revenue), light fill (target zone)
- Target line: Horizontal dashed line, subtle orange
- Data points: Small circles on line (hover for tooltip)
- Animation: Area fills on page load (300ms ease-out)
- Colors: Actual (teal solid), target (orange dashed)

**Interactions:**
- Hover → Show exact values in tooltip (date, actual, target, variance %)
- Click → Drill to transactions page with month filter
- Toggle: Show "Actual only" vs. "Actual + Target"
- Range selector: Last 3/6/12 months

---

### 4. LEAD SOURCE ATTRIBUTION — Channel Performance
**Objective:** Know which lead source converts best  
**Business Value:** Allocate budget to highest-ROI channels  
**Data Source:** `/api/v1/customers` (group by source)  
**Chart Type:** Donut chart with outer ring labels + drill-down  
**Complexity:** Low (1.5-2 hours)  
**ROI Score:** 🟠 HIGH (8/10)

**Wireframe:**
```
WHERE DO YOUR BEST LEADS COME FROM?

        GoogleMaps (32 leads, 25% conv)
             ╭──────────────╮
    ┌─────  │      32      │  ────┐
    │     ╱ ╰──────────────╯ ╲    │
    │   ╱   Scraping (45)    ╲   │
    │ ╱    12% conversion     ╲  │
   ╱  ┌──────────────────────┐  ╲
   │  │      120 LEADS       │  │
   │  │    ↗️ 6.7% → Cust    │  │
   │  └──────────────────────┘  │
    ╲ ╱   Manual (28)         ╱  │
     │ ╲   20% conversion   ╱    │
     │  ╲ ╭──────────────╮ ╱     │
     └──  │      15      │  ──── ┘
          ╰──────────────╯
          Import (15)
          5% conversion

Color coding:
🟢 GoogleMaps (25% conv) — Best
🟠 Manual (20% conv)     — Good
🟡 Scraping (12% conv)   — OK
🔴 Import (5% conv)      — Poor
```

**Design Details:**
- Donut segments: Teal (best), orange (medium), gray (weak)
- Center metric: Total lead count
- Outer ring: Source names, conversion rates
- Animation: Segments slice in on load (200ms stagger)
- Highlight: Best-performing source (brightest color)

**Interactions:**
- Click segment → Filter leads page to show only that source
- Hover → Show: Count, conversion rate, avg lead quality score
- Toggle: View by "Count" vs. "Conversion Rate" vs. "Cost per Lead"
- Drill: View campaigns/imports associated with source

---

### 5. EMAIL CAMPAIGN PERFORMANCE — Sortable Analytics
**Objective:** Track what email content works  
**Business Value:** Optimize future campaigns based on performance  
**Data Source:** `/api/v1/email-campaigns/analytics`  
**Chart Type:** Animated data table (sortable columns) + mini-bars  
**Complexity:** Medium (2.5-3 hours)  
**ROI Score:** 🟠 HIGH (8/10)

**Wireframe:**
```
YOUR LAST 7 CAMPAIGNS

Campaign Name          │ Sent  │ Open% │ Click% │ Status
─────────────────────────────────────────────────────────
"Summer Sale Q2"      │ 250   │ 32%   │ 8%     │ ⚡ Hot
"Product Launch"      │ 180   │ 28%   │ 6%     │ ✓ Good
"Newsletter #4"       │ 320   │ 22%   │ 4%     │ ✓ OK
"Re-engagement"       │ 150   │ 18%   │ 2%     │ △ Low
"Cold outreach"       │ 200   │ 15%   │ 1%     │ ✗ Poor
"VIP exclusive"       │ 85    │ 41%   │ 12%    │ 🔥 Best
"Webinar invite"      │ 300   │ 24%   │ 5%     │ ✓ Good

Mini-bar indicators (Open% visual):
████████░░ 32%
████████░░ 28%
██████░░░░ 22%
█████░░░░░ 18%
██░░░░░░░░ 15%
████████████ 41% ← Best performer

Legend:
⚡ Hot (>30% open)
✓ Good (20-30%)
△ Low (15-20%)
✗ Poor (<15%)
```

**Design Details:**
- Table rows: Hover state (slight background tint, subtle shadow)
- Mini-bars: Teal gradient (percentage fill)
- Status badges: Color-coded (hot, good, low, poor)
- Sortable columns: Indicate sort direction with ↑↓ icons
- Animation: Rows slide in staggered (50ms per row)

**Interactions:**
- Click row → Drill to campaign detail page (recipients, opens, clicks timeline)
- Sort by: Name, Sent, Open%, Click%, Date
- Filter: By status (hot, good, poor) or date range
- Action button: "Resend to non-openers" or "Similar campaign"
- Sparkline on hover: Show open/click timeline over days

---

### 6. EXPENSE BREAKDOWN — Category Analysis
**Objective:** See where money actually goes  
**Business Value:** Identify savings opportunities, control costs  
**Data Source:** `/api/v1/transactions` (expense, grouped by category)  
**Chart Type:** Stacked horizontal bar with value labels  
**Complexity:** Low (1.5-2 hours)  
**ROI Score:** 🟠 HIGH (8/10)

**Wireframe:**
```
MONTHLY EXPENSES BY CATEGORY

This Month (May): R$ 18,200

┌─ Salaries              9,100 (50%)    ████████████████████░░░ R$ 9.1k
│
├─ Tools & Software     3,640 (20%)    ████████░░░░░░░░░░░░░░░░ R$ 3.6k
│  └─ Subcat breakdown
│      • Slack: R$ 1,200
│      • Stripe: R$ 900
│      • Cloud: R$ 1,540
│
├─ Marketing           2,730 (15%)    ██████░░░░░░░░░░░░░░░░░░ R$ 2.7k
│  └─ Ads: R$ 1,500
│     Content: R$ 800
│
├─ Operations          1,460 (8%)     ███░░░░░░░░░░░░░░░░░░░░░ R$ 1.5k
│
└─ Other                 270 (2%)     ░░░░░░░░░░░░░░░░░░░░░░░░ R$ 270

Trend line: Last 3 months
May:   R$ 18.2k
April: R$ 19.5k  ↘ (Down 6%)
March: R$ 17.8k

Alert: Salaries up 8% vs. last month (is this expected?)
```

**Design Details:**
- Main bar: Teal (primary), segments by category
- Colors: Teal (salaries), orange (tools), green (marketing), gray (other)
- Labels: Values overlaid on segments (white text if enough space)
- Mini sparkline: Last 3 months next to category
- Animation: Bars fill left-to-right on load

**Interactions:**
- Click category → Drill to transaction detail page filtered by category
- Expand category → Show subcategories (nested bar)
- Hover → Show detailed breakdown tooltip
- Toggle: View by "Amount" vs. "% of total" vs. "Change MoM"
- Trend: Show last 3/6/12 months comparison

---

### 7. CASH FLOW FORECAST — 3-Month Projection
**Objective:** Predict cash availability and plan ahead  
**Business Value:** Never run out of cash, plan investments confidently  
**Data Source:** `/api/v1/transactions` (actuals) + `/api/v1/monthly-simulations` (projections)  
**Chart Type:** Dual-line chart (actuals + forecast)  
**Complexity:** High (3-4 hours)  
**ROI Score:** 🟠 HIGH (9/10)

**Wireframe:**
```
CASH FLOW — ACTUALS + 3-MONTH FORECAST

     R$ 45k ┤
            │
     R$ 40k ┤              ╭─ Forecast begins
            │          ╱──╱
     R$ 35k ┤      ╱───╱   ╲
            │  ╱──╱         ╲
     R$ 30k ┤═══════════════╲════════════════ Safe level
            │                ╲
     R$ 25k ┤                 ╲╲
            │                  ╲╲
     R$ 20k ┤                   ╲───╮
            │                       ╲
     R$ 15k ┤                        ╲╲
            │                         ╲╲── Projected (Conservative)
     R$ 10k ┤                          ╲╰──
            │
      R$ 5k ┤
            └──────────────────────────────────
             Mar Apr May Jun Jul Aug Sep Oct Nov

Actual (Solid teal):
Mar: 28.5k → Apr: 31.2k → May: 34.8k

Forecast (Dashed teal, with confidence band):
June: 36.5k (±2.1k)
July: 38.2k (±2.8k)
Aug:  36.9k (±3.2k)

Safe Level: R$ 30k (green zone)
Caution: R$ 20-30k (yellow zone)
Danger: <R$ 20k (red zone)

Scenario selector:
• Conservative (current view)
• Moderate (+10% revenue)
• Optimistic (+20% revenue)
```

**Design Details:**
- Actual line: Solid teal, data points marked with circles
- Forecast line: Dashed teal, with confidence band (light teal fill)
- Safe zone: Green background (30k+), caution yellow (20-30k), danger red (<20k)
- Grid: Subtle light gray lines, major gridlines darker
- Animation: Line draws on load (animated SVG stroke-dashoffset)

**Interactions:**
- Hover → Show exact values in tooltip (date, actual/forecast, variance from target)
- Click → Edit assumptions (revenue growth rate, expense changes)
- Scenario toggle: Switch between conservative/moderate/optimistic
- Range selector: Show last 3/6/12 months + 3-month forecast
- Export: Download forecast as CSV

---

### 8. TOP AD CAMPAIGNS — Performance Cards
**Objective:** Validate ad spend is generating ROI  
**Business Value:** Know which campaigns to double down on  
**Data Source:** `/api/v1/ads` (Facebook campaigns)  
**Chart Type:** Card grid (top 3) with mini performance bars  
**Complexity:** Medium (2-3 hours)  
**ROI Score:** 🟡 MEDIUM (7/10)

**Wireframe:**
```
TOP 3 AD CAMPAIGNS (By ROAS)

┌─────────────────────────────────────────────────────┐
│ #1 🔥 "Summer Sale"                                 │
├─────────────────────────────────────────────────────┤
│ ROAS: 4.2x                                          │
│ Spend: R$ 1,200  |  Revenue: R$ 5,040              │
│ Impressions: 45.2k | Clicks: 1,240 (2.7%) |        │
│ Conversions: 89 (7.2%)                              │
│                                                     │
│ Trend: ↗ +15% this week                             │
│                                                     │
│ ├─ Performance: ██████████ 4.2x (Excellent)        │
│ ├─ Efficiency:  ████████░░ 8.2 CTR (Good)          │
│ └─ Conversions: ██████████ 89 (Optimal)            │
│                                                     │
│ [▶ View details] [📊 Analytics] [⚙️ Duplicate]      │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ #2 "Product Launch"                                 │
├─────────────────────────────────────────────────────┤
│ ROAS: 3.1x  |  Spend: R$ 2,800  |  Rev: R$ 8,680   │
│ ├─ Performance: ████████░░ 3.1x                     │
│ └─ Status: ↘ -8% (Monitor)                          │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ #3 "Retargeting"                                    │
├─────────────────────────────────────────────────────┤
│ ROAS: 2.1x  |  Spend: R$ 950  |  Rev: R$ 1,995     │
│ ├─ Performance: █████░░░░░░ 2.1x                    │
│ └─ Status: ✓ Stable                                 │
└─────────────────────────────────────────────────────┘

All campaigns (right sidebar):
5 campaigns total | 3 active | 2 paused
Avg ROAS: 3.1x
Total spend: R$ 4,950
Total revenue: R$ 15,715
```

**Design Details:**
- Card layout: 3 columns desktop, stacked mobile
- Top card: Full detail (highlighted with border accent)
- Mini cards: Condensed view
- Mini-bars: Teal (performance), orange (caution), red (poor)
- Badges: 🔥 for top performer, ↗️ for trending up, ⚠️ for declining
- Animation: Cards slide up on load (100ms stagger)

**Interactions:**
- Click card → Drill to campaign detail page (daily breakdown, creative, audience)
- Action buttons: View details, analytics, duplicate, pause
- Toggle: View by ROAS, spend, impressions, conversions
- Filter: Active only, paused, all
- Comparison: Select 2-3 campaigns to compare side-by-side

---

### 9. FINANCIAL GOALS PROGRESS — Radial Indicators
**Objective:** Visualize progress toward multiple financial targets  
**Business Value:** Stay motivated, track milestones  
**Data Source:** `/api/v1/financial-goals`  
**Chart Type:** Radial progress indicators (multiple rings)  
**Complexity:** Medium (2-3 hours)  
**ROI Score:** 🟡 MEDIUM (7/10)

**Wireframe:**
```
YOUR FINANCIAL GOALS

┌────────────┬────────────┬────────────┐
│   SAVE     │  REVENUE   │  EXPENSES  │
│  R$ 5,000  │   R$ 50k   │   < R$ 15k │
│            │ (Quarterly)│ (Monthly)  │
│    ███░░   │    ███░░   │    █░░░░░  │
│    38%     │    56%     │    67%     │
│ +R$ 1,9k   │ +R$ 28k    │ -R$ 2.2k   │
│  10mo left │  6mo left  │  7mo left  │
│    ✓ On    │    ⚠️ Slow │    🔥 Fast│
│   track    │  (add R$7k)│ (Cutting it│
│            │            │ close!)    │
└────────────┴────────────┴────────────┘

Goal detail (on hover):
REVENUE Q2 2026: R$ 50,000
├─ Jan: R$ 15.2k
├─ Feb: R$ 18.1k  ↗
└─ May: R$ 24.5k (projected)

Projection: Hit goal by July (1 month late)
Alert: Pace needs +R$ 7k/month to stay on track
```

**Design Details:**
- Radial progress: Teal ring for progress, light gray for remaining
- Center text: Goal name + current amount
- Outer ring: Target amount + deadline
- Status badge: ✓ on track, ⚠️ slow, 🔥 fast
- Color coding: Teal (on track), orange (slow), green (ahead)
- Animation: Ring fills clockwise on load (300ms)

**Interactions:**
- Click ring → Show detailed goal page (edit, add entries, projections)
- Hover → Show tooltip with progress details and deadline
- Monthly breakdown: Click to expand and see month-by-month progress
- Edit goal: Change target, deadline, or visibility

---

### 10. LEAD SEGMENT DISTRIBUTION — Engagement Status
**Objective:** Know your lead quality distribution (Cold, Warm, Hot)  
**Business Value:** Prioritize follow-up efforts, focus on warm/hot leads  
**Data Source:** `/api/v1/customers` (group by segment)  
**Chart Type:** Stacked horizontal bar with inline labels  
**Complexity:** Low (1.5-2 hours)  
**ROI Score:** 🟢 NICE (6/10)

**Wireframe:**
```
LEAD QUALITY DISTRIBUTION

Current (120 leads):
Cold  │██████░░░░│ 45 leads (37%)  → 5-10% conversion
Warm  │████████░░│ 48 leads (40%)  → 15-20% conversion  ⭐
Hot   │███░░░░░░░│ 27 leads (23%)  → 40-50% conversion  🔥

Last 3 months trend:
May:  Cold 40% | Warm 38% | Hot 22%
April: Cold 45% | Warm 35% | Hot 20%
March: Cold 50% | Warm 32% | Hot 18%

Insight: ↗️ More leads warming up (Cold -13%, Hot +5%)

Action items:
• Focus outreach on 27 Hot leads (6 week turnaround)
• Nurture 48 Warm leads (email campaign)
• Re-evaluate 45 Cold leads (qualify or move to nurture list)

[📧 Send Warm campaign] [🎯 Focus on Hot] [📊 View details]
```

**Design Details:**
- Stacked bar: Cold (gray), Warm (orange), Hot (teal/green)
- Labels: Inline count + percentage
- Trend line: Last 3 months showing segment shift
- Color intensity: Increases with engagement quality
- Animation: Segments fill on load (150ms per segment)

**Interactions:**
- Click segment → Filter leads page to show only that segment
- Hover → Show conversion rate range and recommended actions
- Trend toggle: Show last 3/6/12 months
- Action buttons: Send campaign (warm), focus on (hot), re-qualify (cold)

---

## ✨ HERO DASHBOARD LAYOUT (Full Page Wireframe)

```
╔════════════════════════════════════════════════════════════════════╗
║ DIAX Dashboard                    📊 Jun 8 | Welcome, Alexandre   ║
║ Last updated: 2 minutes ago       [⚙️ Settings] [🔔 Alerts] [👤]  ║
╚════════════════════════════════════════════════════════════════════╝

[STICKY HEADER — Quick Navigation]
📊 Dashboard | 🎯 Leads | 💰 Finance | 📧 Email | 📺 Ads | ⚙️ Settings

[HERO ROW — 4 Key Metrics (Span full width, 4 equal columns)]
┌──────────────┬──────────────┬──────────────┬──────────────┐
│ REVENUE      │ GROWTH       │ GOALS        │ FORECAST     │
│ R$ 24.5k     │ +12.3%       │ 6/18 (33%)   │ Next 45d     │
│ 82% of goal  │ MoM ↗        │ On track ✓   │ R$ 28.2k     │
│ -18% ⚠️      │ 3-mo trend   │              │ +16% proj    │
│ [Details]    │ [Details]    │ [Details]    │ [Details]    │
└──────────────┴──────────────┴──────────────┴──────────────┘

[CRITICAL SECTION — What needs action NOW]
┌──────────────────────────────────────────────────────────────────┐
│ 🚨 ACTION ITEMS (3 things to do right now)                       │
├──────────────────────────────────────────────────────────────────┤
│ • 5 leads without contact > 7 days → [Follow up now]             │
│ • 2 negotiations stalled > 14 days → [Check status]              │
│ • Expense tracking behind (manual input needed) → [Update now]   │
└──────────────────────────────────────────────────────────────────┘

[ROW 2 — SALES PIPELINE (2/3 width) + EMAIL QUICK VIEW (1/3)]
┌────────────────────────────────────────┬──────────────────┐
│ #2 LEAD FUNNEL (Waterfall)             │ #5 EMAIL PERF    │
│ 120→85→45→12→8 customers               │ Last 7 campaigns │
│ Bottleneck: Qualified→Negotiating      │                  │
│ [Click to view leads in each stage]    │ Hot: ████████░░  │
│ [Drill to leads page]                  │ Good: ████░░░░░░ │
│                                        │ Low: ██░░░░░░░░░ │
│                                        │ [See all]        │
└────────────────────────────────────────┴──────────────────┘

[ROW 3 — REVENUE & FINANCIAL (1/2 width each)]
┌────────────────────────────────┬────────────────────────────────┐
│ #3 REVENUE TREND (6 months)    │ #9 GOAL PROGRESS (Multiple)    │
│ ↗ Upward trend, on pace        │ ✓ Save: 38% (10mo left)        │
│ Area chart with target line    │ ⚠️ Revenue: 56% (6mo left)     │
│ [Interactive: hover, toggle]   │ 🔥 Expenses: 67% (7mo left)    │
│                                │ [View goal details]            │
└────────────────────────────────┴────────────────────────────────┘

[ROW 4 — CHANNELS & SEGMENTS (1/2 width each)]
┌────────────────────────────────┬────────────────────────────────┐
│ #4 LEAD SOURCE (Donut)         │ #10 LEAD SEGMENTS (Stacked)    │
│ GoogleMaps: 28% (Best!)        │ Cold: 37% | Warm: 40% | Hot: 23%
│ Scraping: 42%                  │ Trend: More warming up ↗       │
│ Manual: 20%                    │ Focus: 27 Hot leads            │
│ Import: 10%                    │ [Send warm campaign]           │
│ [Click source to filter leads] │ [View all segments]            │
└────────────────────────────────┴────────────────────────────────┘

[ROW 5 — EXPENSES & ADS (Split view)]
┌────────────────────────────────┬────────────────────────────────┐
│ #6 EXPENSES (Stacked bar)      │ #8 TOP ADS (Card grid, 3 top)  │
│ Salaries: 50%                  │ 🔥 Summer Sale (ROAS: 4.2x)    │
│ Tools: 20%                     │ ✓ Product Launch (ROAS: 3.1x)  │
│ Marketing: 15%                 │ Retargeting (ROAS: 2.1x)       │
│ Other: 15%                     │ [View all campaigns]           │
│ Total: R$ 18.2k (Stable ↔)     │ [Create new campaign]          │
└────────────────────────────────┴────────────────────────────────┘

[ROW 6 — FORECAST & DETAILS (Full width, collapsible)]
┌────────────────────────────────────────────────────────────────┐
│ #7 CASH FLOW FORECAST (3-month projection)                     │
│ ↗ Conservative: R$ 36.5k by July                               │
│ Safe zone maintained throughout (green area)                   │
│ [Toggle scenarios: Conservative/Moderate/Optimistic]           │
│ [Drill to detailed projections]                                │
└────────────────────────────────────────────────────────────────┘

[BOTTOM — Footer with metadata]
Data refresh: Real-time (websocket) | Last sync: 2min ago | Timezone: BRT
```

---

## 🎬 MICROINTERACTIONS & UX PREMIUM

### Entrance Animations
```
1. Page load sequence (300ms total, staggered):
   - Hero metrics: Fade in + subtle scale (100ms)
   - Action items: Slide from left (100ms stagger per item)
   - Charts: Bars/lines draw, areas fill (200ms per chart)
   
2. Scroll reveal (IntersectionObserver):
   - Lazy-load below-fold charts
   - Animate on scroll into view
   - No animation if prefers-reduced-motion
```

### Hover States
```
1. Cards & Chart containers:
   - Background: Subtle tint (+2% lightness)
   - Shadow: Elevate slightly (0 8px 16px rgba)
   - Transition: 150ms ease-out
   
2. Data points on charts:
   - Hover circle: Enlarge slightly, highlight
   - Tooltip: Fade in, show exact values
   - Related data: Highlight connected points
   
3. Links & CTAs:
   - Underline appears (smooth animation)
   - Color brightens slightly
   - Icon animates (small rotation or move)
```

### Number Counters
```
1. When metrics load/update:
   - Current value → Target value
   - Animated over 300ms (easeOutQuad)
   - Example: 24.5k updating to 24.8k
   - Triggers on: Page load, auto-refresh (every 5min)
   
2. Percentage indicators:
   - Counter updates smoothly
   - Color shift if threshold crossed (80% → ⚠️ yellow)
```

### Loading States
```
1. Skeleton loaders:
   - Match exact shape of content
   - Pulsing gradient animation (500ms loop)
   - Example: Hero metrics skeleton (4 cards)
   
2. Chart loading:
   - Fade in placeholder axis
   - Animate data series in sequence
   - No placeholder for sub-second loads
```

### Interactions (Drill-downs)
```
1. Click chart stage/segment:
   - Page transition: Fade out dashboard, fade in detail page
   - Breadcrumb: Show "Dashboard > [Chart] > [Selected item]"
   - Pre-filter: New page loads with filter applied
   
2. Hover drill hint:
   - Subtle cursor change (pointer for clickables)
   - Tooltip: "Click to view [items]"
   
3. Drill completion:
   - Back button: Fade back to dashboard
   - State preservation: Return to exact scroll position
```

### Empty States & Errors
```
1. No data yet:
   - Illustration (minimal, 200x200px)
   - Heading: "No [metric] yet"
   - Copy: "When you [action], it will appear here"
   - CTA: "[Action] now"
   
2. Data load error:
   - Error icon + message
   - Retry button (calls endpoint again)
   - Contact support link (if persistent)
```

---

## 🚀 QUICK WINS — 10 Features in <1 Day Each

### Tier 1 — 2-4 Hours Each (Highest Impact)

**1. Hero Revenue Metric + Sparkline**
- Extract data from existing `FinanceSummaryWidget`
- Build circular progress component (Recharts)
- Add sparkline showing 6-month trend
- **Status:** Ready today

**2. Lead Funnel Waterfall**
- Call `/api/v1/customers` with pageSize=1000
- Group by status, calculate counts + %
- Build Recharts waterfall chart
- Add click-to-drill to leads page
- **Status:** Ready today

**3. Lead Source Donut Chart**
- Group customers by `Customer.Source`
- Calculate conversion rates (status=4 / total)
- Build Recharts pie chart with custom labels
- **Status:** Ready today

**4. Expense Breakdown Bar**
- Call `/api/v1/transactions?type=expense` for current month
- Group by category, sum amounts
- Build Recharts horizontal stacked bar
- Add category drill-down
- **Status:** Ready today

### Tier 2 — 4-6 Hours Each (High Value)

**5. Revenue Trend Area Chart**
- Call `/api/v1/transactions` for last 6 months
- Aggregate by month (sum income)
- Build Recharts area chart with dual axis (actual + target line)
- Add date range selector
- **Status:** Ready in 4-6 hours

**6. Email Campaign Table**
- Call `/api/v1/email-campaigns/analytics?days=30`
- Fetch last 7-10 campaigns
- Build sortable data table (TanStack Table)
- Add status badges and mini-bars
- **Status:** Ready in 4-6 hours

**7. Top Ad Campaigns Cards**
- Call `/api/v1/ads` for top campaigns by ROAS
- Limit to top 3
- Build card grid with performance indicators
- **Status:** Ready in 4-6 hours

**8. Action Items / Alerts Widget**
- Hardcode 3-5 high-priority alerts (for MVP)
- Build dismissible alert cards
- Add quick-action buttons
- **Future:** Wire up backend `/api/v1/dashboard/alerts` endpoint (see Phase 2)
- **Status:** Ready today (hardcoded), dynamic in Phase 2

### Tier 3 — 6-8 Hours Each (Polish & Completeness)

**9. Cash Flow 3-Month Forecast**
- Fetch `/api/v1/transactions` + `/api/v1/monthly-simulations`
- Combine actuals + forecast
- Build Recharts dual-line chart with confidence band
- Add scenario selector (conservative/moderate/optimistic)
- **Status:** Ready in 6-8 hours

**10. Goal Progress Rings**
- Call `/api/v1/financial-goals`
- Build radial progress indicators (Recharts RadialBarChart or custom SVG)
- Show progress % and deadline
- **Status:** Ready in 6-8 hours

---

## 📋 IMPLEMENTATION ROADMAP (3 Phases)

### Phase 1: Foundation (3-5 days) — MVP Dashboard
**Goal:** Launch 5 core charts answering "What's working?" + "Where am I losing money?"

**Charts to build:**
1. ✅ Revenue vs. Goal (hero)
2. ✅ Lead Funnel (waterfall)
3. ✅ Lead Source (donut)
4. ✅ Expense Breakdown (stacked bar)
5. ✅ Quick Alerts (hardcoded widget)

**Technical:**
- Refactor `/dashboard/page.tsx` layout
- Create 5 new widget components
- Add Recharts charting library (if not present)
- Implement hover states + basic animations
- Remove low-priority widgets (shopping list, snippets, etc.)

**Metrics:**
- Dashboard load time: <2s
- 5 charts fully interactive (click to drill)
- Mobile responsive (stacked layout)

**Deliverable:** `dashboard/page.tsx` v2 with 5 charts

---

### Phase 2: Expansion (3-5 days) — Enhanced Insights
**Goal:** Add 5 more charts for "What's the trend?" + "What are my best channels?"

**Charts to build:**
6. ✅ Revenue Trend (area chart, 6-month)
7. ✅ Email Campaign Performance (sortable table)
8. ✅ Top Ad Campaigns (card grid)
9. ✅ Cash Flow Forecast (dual-line chart)
10. ✅ Goal Progress Rings (multiple radial)

**Technical:**
- Build data aggregation helpers (e.g., `groupTransactionsByMonth`)
- Implement advanced filtering (date ranges, toggles)
- Add loading skeleton states
- Optimize API calls (batch requests, caching)
- Implement drill-down navigation
- Add date range selector (global or per-chart)

**Backend enhancements (optional):**
- Create `/api/v1/dashboard/alerts` endpoint (for dynamic alerts)
- Enhance `/api/v1/finance/summary` to support monthly trends
- Cache expensive aggregations (5-min TTL)

**Metrics:**
- 10 charts fully functional
- Dashboard load <1.5s (with caching)
- All charts have drill-down capabilities
- Mobile fully responsive

**Deliverable:** Full dashboard with 10 charts + drill-downs

---

### Phase 3: Polish & Premium (2-4 days) — Production Grade
**Goal:** Exceptional craft, microinteractions, performance, accessibility

**Polish tasks:**
- ✅ Add entrance animations (staggered reveal)
- ✅ Implement hover micro-interactions (elevate cards, highlight data)
- ✅ Add animated number counters (metrics update smoothly)
- ✅ Optimize chart rendering (lazy load, code split)
- ✅ Empty states + error handling (with illustrations)
- ✅ Accessibility audit (WCAG AA minimum)
- ✅ Dark mode support (if desired)
- ✅ Keyboard navigation (full dashboard usable via keyboard)
- ✅ Mobile optimization (portrait/landscape, touch targets)
- ✅ Prefers-reduced-motion respect (disable animations for accessibility)

**Optional enhancements:**
- Auto-refresh every 5 minutes (with toast notification)
- Export dashboard as PDF
- Save custom dashboard layout
- Share dashboard snapshot (URL)
- Notifications for alerts (email, browser)

**Performance targets:**
- First Contentful Paint: <1s
- Dashboard load: <1.5s
- Chart interaction latency: <100ms
- Lighthouse score: 95+

**Deliverable:** Production-ready dashboard with premium craft

---

## 📊 CHART ROI RANKING

| Rank | Chart | Impact | Complexity | Effort | ROI | Time to Build |
|------|-------|--------|-----------|--------|-----|----------------|
| 1 | Revenue vs. Goal | 🔴 Critical | Low | 2h | 10/10 | <1 day |
| 2 | Lead Funnel | 🔴 Critical | Low | 3h | 10/10 | <1 day |
| 3 | Revenue Trend | 🔴 Critical | Medium | 5h | 9/10 | <1 day |
| 4 | Lead Source | 🟠 High | Low | 2h | 8/10 | <1 day |
| 5 | Email Campaigns | 🟠 High | Medium | 5h | 8/10 | <1 day |
| 6 | Expense Breakdown | 🟠 High | Low | 2h | 8/10 | <1 day |
| 7 | Cash Flow Forecast | 🟠 High | High | 6h | 9/10 | 1-2 days |
| 8 | Top Ad Campaigns | 🟡 Medium | Medium | 5h | 7/10 | <1 day |
| 9 | Goal Rings | 🟡 Medium | Medium | 5h | 7/10 | <1 day |
| 10 | Lead Segments | 🟢 Nice | Low | 2h | 6/10 | <1 day |

**Recommendation:** Build in order of ROI. Phase 1 = Top 6. Phase 2 = Remaining 4.

---

## 🛠️ Technical Stack

**Charting:**
- Recharts (bar, line, area, pie, waterfall, radial)
- Nivo (alternative for advanced charts)
- Custom SVG for simple shapes

**Animations:**
- Framer Motion (entrance, hover, transitions)
- Recharts built-in animations (chart data reveal)
- CSS transitions (150-300ms, ease-out curves)

**Data Fetching:**
- Existing services: `customers.ts`, `finance.ts`, `leads.ts`, etc.
- Batch requests where possible (Promise.all)
- Cache with SWR or React Query (5-10 min TTL)

**Components:**
- shadcn/ui (buttons, cards, dropdowns, tables)
- Lucide React (icons)
- Tailwind CSS (styling, responsive)

**Performance:**
- Lazy load charts (IntersectionObserver)
- Code-split dashboard pages
- Memoize expensive computations (useMemo)
- Skeleton loaders for sub-2s loads

---

## ✅ Success Criteria

- [ ] All 10 charts rendering with real data
- [ ] Dashboard load time <2s on first load
- [ ] Charts interactive (click to drill, hover for details)
- [ ] Mobile responsive (portrait and landscape)
- [ ] Animations smooth (no jank, 60fps)
- [ ] Accessibility (WCAG AA minimum, keyboard navigation)
- [ ] No console errors or warnings
- [ ] User can answer "What should I do?" in <10 seconds
- [ ] All drill-downs work (charts → detail pages)
- [ ] Empty states + error states handled gracefully

---

## 📝 File Structure

```
crm-web/src/
├── components/dashboard/
│   ├── dashboard-page.tsx (MAIN — new layout)
│   ├── hero/
│   │   ├── RevenueHeroWidget.tsx (Chart #1)
│   │   ├── MetricCard.tsx (Revenue, Growth, Goals, Forecast)
│   │   └── AlertsWidget.tsx (Quick actions)
│   ├── charts/
│   │   ├── LeadFunnelChart.tsx (Chart #2)
│   │   ├── LeadSourceChart.tsx (Chart #4)
│   │   ├── RevenueTrendChart.tsx (Chart #3)
│   │   ├── ExpenseBreakdownChart.tsx (Chart #6)
│   │   ├── EmailPerformanceTable.tsx (Chart #5)
│   │   ├── TopAdsCards.tsx (Chart #8)
│   │   ├── CashFlowForecastChart.tsx (Chart #7)
│   │   ├── GoalProgressRings.tsx (Chart #9)
│   │   └── LeadSegmentChart.tsx (Chart #10)
│   └── shared/
│       ├── ChartTooltip.tsx
│       ├── ChartLegend.tsx
│       ├── LoadingSkeleton.tsx
│       └── EmptyState.tsx
├── services/
│   ├── dashboard.ts (Aggregation helpers)
│   └── [existing services already there]
└── types/
    └── dashboard.ts (New types for dashboard DTOs)
```

---

## 🎓 Design System Consistency

Every chart follows DESIGN.md:
- **Colors:** Primary (teal) for main metric, secondary (blue) for alerts, accent (orange) for highlights
- **Typography:** Inter 16px body, 24px headings
- **Spacing:** 16px cards, 24px sections (grid-based)
- **Motion:** 300ms ease-out-quad on load, 150ms on interaction
- **Elevation:** 1px borders, subtle shadows (0 1px 3px...)
- **No anti-patterns:** No pie charts, gradients, nested cards, or hero-metric templates

---

## 🚀 Getting Started (Tomorrow)

**Day 1 (4-6 hours):**
1. Create 5 new widget components (heroes, funnel, source, expense, alerts)
2. Refactor dashboard layout
3. Wire up data fetching
4. Add Recharts if not present
5. Test on mobile

**Day 2 (4-6 hours):**
1. Build remaining 5 charts
2. Add drill-down navigation
3. Implement loading skeletons
4. Add hover states

**Day 3 (2-4 hours):**
1. Polish animations
2. Accessibility review
3. Performance optimization
4. Launch to production

---

## 📞 Questions?

- **"Which chart first?"** → Revenue vs. Goal (hero). It's the north star.
- **"New endpoint needed?"** → No. All data exists in 71 controllers.
- **"Mobile ready?"** → Yes. All charts responsive-first.
- **"Animation tools?"** → Framer Motion + Recharts built-in anims + CSS.
- **"How long total?"** → Phase 1: 3-5 days, Phase 2: 3-5 days, Phase 3: 2-4 days.

---

**Ready to transform DIAX CRM into a visual intelligence engine?**

Next step: `/impeccable shape dashboard-hero` to design the hero section in detail, OR proceed directly to implementation.

