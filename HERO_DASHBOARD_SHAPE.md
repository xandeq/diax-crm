# DIAX Dashboard — Premium Hero Section Shape Design

## 🎯 Design Goal

Transform 4 metrics into a **visual story that answers 5 critical business questions in <10 seconds**.

**Questions to answer:**
1. O que está funcionando? (What's working?)
2. O que está quebrado? (What's broken?)
3. Onde estou perdendo dinheiro? (Where am I losing money?)
4. O que devo fazer agora? (What should I do now?)
5. Qual ação gera mais receita? (Which action generates revenue?)

---

## 📐 Layout Architecture

### Grid System
- **12-column responsive grid** (Tailwind default)
- **Gutters:** 16px / 24px
- **Hero section:** 1 row, full width, 4 equal columns

### Responsive Breakpoints
```
Mobile (0-639px):   1 column (stack vertically)
Tablet (640-1023px): 2 columns (2x2 grid)
Desktop (1024px+):  4 columns (1 hero row)
```

---

## 🖼️ The 4 Hero Metric Cards

### Card Layout Template
```
┌──────────────────────────────────┐
│ LABEL (12px, uppercase)          │
│                                  │
│ [PRIMARY VALUE — Large & Bold]   │
│                                  │
│ [Context / Sublabel]             │
│ [Trend / Alert indicator]        │
│                                  │
│ [Optional: Mini visualization]   │
│                                  │
│ [CTA Link]                       │
└──────────────────────────────────┘
```

---

### Card 1: REVENUE THIS MONTH

**Question it answers:** "O que está funcionando?" + "Onde estou perdendo dinheiro?"

```
┌──────────────────────────────────┐
│ REVENUE THIS MONTH               │ ← 12px label, uppercase, fg-secondary
│                                  │
│         R$ 24,500                │ ← 32px monospace, bold, fg-primary
│         vs R$ 30,000 goal        │ ← 14px, fg-secondary (context)
│                                  │
│ ↗ +3.2% from last month          │ ← 14px, green (positive trend)
│ ⚠️ -18% from goal                │ ← 14px, orange (caution alert)
│                                  │
│ [Sparkline: last 6 months]       │ ← 40px height, teal area, no axis
│ ↗↗↗↗↗ Upward trend visible       │
│                                  │
│ [More details →]                 │ ← 14px, teal link, underline on hover
└──────────────────────────────────┘
```

**Design details:**
- Background: oklch(96% 0.004 189°)
- Border: 1px solid oklch(85% 0.004 189°)
- Shadow: 0 1px 3px rgba(0,0,0,0.08)
- Padding: 24px
- Sparkline: Recharts AreaChart (simplified, no tooltip by default)
- Color psychology: Teal (primary) for main value, green for positive trend, orange for alert

---

### Card 2: MONTH-OVER-MONTH GROWTH

**Question it answers:** "Qual ação gera mais receita?"

```
┌──────────────────────────────────┐
│ MONTH-OVER-MONTH GROWTH          │ ← Label
│                                  │
│           +12.3%                 │ ← 48px, bold, color: green (positive)
│                                  │   (color: red if negative)
│                                  │
│ Revenue trending up ✓            │ ← 14px insight, conditional text
│ Last 3 months: +3.2%, +2.8%, +1.5%│
│                                  │
│ [View 12-month trend →]          │ ← CTA link
└──────────────────────────────────┘
```

**Design details:**
- Large % metric (48px) is focal point
- Color conditional: Green (>0%), Red (<0%), Orange (stalled)
- Mini trend line: Last 3 months summary (text-based for simplicity)
- Insight: 1-2 sentences explaining what it means
- Animation: % value animates on page load (e.g., 0% → 12.3% over 300ms)

---

### Card 3: GOAL PROGRESS

**Question it answers:** "Onde estou perdendo dinheiro?" + "O que está quebrado?"

```
┌──────────────────────────────────┐
│ PROGRESS TO GOAL                 │ ← Label
│                                  │
│    ████████░░░░ 82%              │ ← Progress bar (full width, teal fill)
│                                  │   Height: 8px, border-radius: 4px
│ R$ 24.5k of R$ 30k               │ ← 14px monospace, context
│ Needs R$ 5.5k more (18% gap)     │ ← 14px, conditional color
│                                  │    - Green: >90% progress
│                                  │    - Orange: 70-90%
│                                  │    - Red: <70%
│                                  │
│ Days left: 23 days               │ ← 12px, label
│ Pace: -18% behind schedule       │ ← 14px, conditional alert
│                                  │
│ [Adjust goal] [View forecast]    │ ← 12px button links
└──────────────────────────────────┘
```

**Design details:**
- Progress bar: CSS progress element or custom div with width: 82%
- Fill color: conditional (green if ≥80%, orange if 60-80%, red if <60%)
- Gap messaging: "Needs R$ X more" makes it actionable
- Days remaining: Adds urgency (color: red if <15 days remaining)
- CTAs: "Adjust goal" (modal) and "View forecast" (drill-down)

---

### Card 4: WHAT TO DO NOW

**Question it answers:** "O que devo fazer agora?"

```
┌──────────────────────────────────┐
│ WHAT TO DO NOW                   │ ← Label
│                                  │
│    3 items need attention        │ ← 24px, bold (count)
│                                  │
│ 🔥 1 URGENT (Error badge)        │ ← Red badge, 12px
│    • 2 negotiations stalled >14d │ ← 13px, monospace bullet
│                                  │
│ ⚠️ 2 WARNINGS (Warning badge)    │ ← Orange badge, 12px
│    • 5 leads without contact >7d │ ← 13px, monospace bullet
│    • Expenses not tracked        │
│                                  │
│ [View all →] [Dismiss all]       │ ← 12px button links
└──────────────────────────────────┘
```

**Design details:**
- Badges: 12px, rounded, solid background + white text
  - 🔥 Error (red): oklch(55% 0.15 25°)
  - ⚠️ Warning (orange): oklch(60% 0.14 75°)
  - ✓ Success (green): oklch(65% 0.12 155°)
- Item count: Large, bold, draws attention
- Item bullets: Monospace (13px) for clarity
- CTAs: "View all" (drill to details), "Dismiss all" (clear notifications)
- Animation on load: Items slide in staggered (50ms between each)

---

## 🎬 Animation Sequence

### Page Load (Staggered Entrance)

```
Timeline:
0ms:    Hero section background fades in (opacity 0→1, 100ms)
        Hero section header text fades in

100ms:  Card 1 (Revenue) enters:
        • Fade in (opacity 0→1, 200ms)
        • Scale in (scale 0.95→1.0, 200ms)
        • Transform origin: top-left
        • Easing: cubic-bezier(0.34, 1.56, 0.64, 1) [ease-out-back]

130ms:  Card 2 (Growth) enters:
        • Same animation, offset +30ms

160ms:  Card 3 (Goal) enters:
        • Same animation, offset +30ms

190ms:  Card 4 (Actions) enters:
        • Same animation, offset +30ms

200ms:  Metric animations start:
        • Revenue sparkline: SVG stroke animation (200ms, draws left-to-right)
        • Growth %: Counter animates (0% → actual %, 300ms, easeOutQuad)
        • Goal progress bar: Width animates (0% → actual %, 300ms, easeOutQuad)
        • Action badges: Pop in with slight scale (1.0→1.0, 150ms)

All transforms use GPU acceleration:
  • will-change: opacity, transform
  • transform: translateZ(0)
```

### Hover States (150ms transition)

```
Card Hover:
  • Background: oklch(94% 0.004 189°) [+2% lightness]
  • Shadow: 0 8px 16px rgba(0,0,0,0.12) [elevated]
  • Cursor: pointer
  • Transition: all 150ms cubic-bezier(0.25, 0.46, 0.45, 0.94)
  • Transform: none (no scale to avoid layout shift)

Button/Link Hover:
  • Color: oklch(56% 0.16 189°) [slightly brighter teal]
  • Underline: appear (border-bottom, 2px, teal)
  • Icon: animate right (translateX(2px) + rotate(45°), 150ms)
  • Transition: all 150ms ease-out

Data viz hover (sparkline):
  • Highlight nearest data point
  • Show tooltip (fade in, 100ms)
  • Line opacity: increase
```

### Number Counter Animation

When metric value updates (real-time or refresh):
```
Example: Revenue updates 24.2k → 24.5k

  • Duration: 300ms
  • Easing: easeOutQuad (cubic-bezier(0.25, 0.46, 0.45, 0.94))
  • Format: Comma-separated (R$ 24,500)
  • Increment logic: Calculate delta, divide by frame count, increment per frame
  • When triggered: On page load, on 5-min auto-refresh, on manual refresh click
```

### Loading States

Before data loads:
```
Skeleton loader:
  • Match exact card shape (24px padding, 12px border-radius)
  • Pulsing background animation: opacity 0.3 → 0.6 → 0.3 (loop 1.5s)
  • Color: oklch(91% 0.003 189°) [lighter tint]
  • No text, just shape placeholders

If load time > 500ms:
  • Show skeleton
  • Smooth fade-in of actual content when ready

If load time < 500ms:
  • Skip skeleton (avoid flash)
  • Fade in final content directly
```

---

## 🌈 Color Strategy (Committed Model)

### Primary Teal (oklch(52% 0.15 189°))
- Card borders, CTA links, progress bar fills
- Sparkline area (solid, no gradient)
- Used in 40-50% of visible hero area
- Conveys data-driven, professional, trustworthy

### Secondary Blue (oklch(55% 0.13 245°))
- Positive trend indicators (↗ arrows, "upward" text)
- Secondary CTAs (ghost buttons)
- Used sparingly (~10%)

### Accent Orange (oklch(58% 0.16 35°))
- Warning badges, caution alerts
- "⚠️" status indicators
- Progress bar if variance 60-80%
- Used for attention/caution (~8%)

### Success Green (oklch(65% 0.12 155°))
- "✓" checkmarks, "On track" badges
- Progress bar if variance ≥80%
- Positive percentages (growth)
- Used for positive states (~5%)

### Error Red (oklch(55% 0.15 25°))
- "🔥" critical badges
- Progress bar if variance <60%
- Negative percentages (losses)
- Used for critical alerts (~5%)

### Neutrals (Tinted Teal)
- Text primary: oklch(12% 0.005 189°) — Almost black, teal tint
- Text secondary: oklch(48% 0.006 189°) — Gray, teal tint
- Text tertiary: oklch(65% 0.005 189°) — Light, teal tint
- Borders: oklch(85% 0.004 189°) — Very light
- Background: oklch(96% 0.004 189°) — Off-white, teal tint
- Page bg: oklch(99% 0.003 189°) — Almost white, teal tint

---

## 📱 Responsive Adaptations

### Mobile (< 640px)
```
Grid: 1 column (full width)
Cards: Padding 16px (reduced from 24px)
Font: Scale -10% (headings 22px, body 14px)
Height: Auto (content-driven, not fixed)
Sparkline: 30px height (reduced)
Buttons: Full width (easier touch targets, 44px min height)

Visual order stays the same:
- Card 1: Revenue
- Card 2: Growth
- Card 3: Goal
- Card 4: Actions
```

### Tablet (640-1023px)
```
Grid: 2 columns (2x2 layout)
Cards: 50% width, padding 16px
Font: Standard (no scaling)
Sparkline: 40px height
Shadow: Subtle (same as desktop)

Layout:
┌─────────────┬─────────────┐
│ Revenue     │ Growth      │
├─────────────┼─────────────┤
│ Goal        │ Actions     │
└─────────────┴─────────────┘
```

### Desktop (> 1024px)
```
Grid: 4 columns (full row)
Cards: 25% width, padding 24px
Font: Full scale
Sparkline: 40px height
Shadow: Full elevation

Layout:
┌────────┬────────┬────────┬────────┐
│Revenue │ Growth │ Goal % │Actions │
└────────┴────────┴────────┴────────┘
```

---

## 🎯 Visual Hierarchy (Order of Eye Focus)

1. **Card 1 (Revenue):** Leftmost, largest metric value (32px), sparkline
   - What: Core business metric
   - Answer: "What's working?" or "Where am I losing money?"
   - Focus: Absolute value + trend

2. **Card 2 (Growth):** Large % metric (48px), color-coded
   - What: Momentum indicator
   - Answer: "Which action generates revenue?"
   - Focus: Percentage change + insight

3. **Card 3 (Goal %):** Progress bar dominates (8px thick, full width)
   - What: Goal alignment metric
   - Answer: "Where am I losing money?" (if gap is large)
   - Focus: Visual progress + gap messaging

4. **Card 4 (Actions):** Badge colors + item count (24px)
   - What: Priority/urgency indicator
   - Answer: "What do I do now?"
   - Focus: Severity (red > orange > green)

---

## ✅ Implementation Checklist

- [ ] Use shadcn/ui Card component (border, shadow, padding)
- [ ] Monospace font for metric values (Fira Code or Menlo, 32px)
- [ ] Progress bar using CSS progress or custom div with width: %
- [ ] Sparkline using Recharts AreaChart (no axes, simplified)
- [ ] Colors use OKLCH values from DESIGN.md (not hex)
- [ ] Animations using Framer Motion (Motion.div with variants)
- [ ] Skeleton loader matches card shape (use react-loading-skeleton or custom)
- [ ] Responsive grid: `grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4`
- [ ] Hover states on card container (not child elements)
- [ ] Badges with conditional colors (green/orange/red logic)
- [ ] CTAs are semantic links with focus states
- [ ] Respects `prefers-reduced-motion` (disable animations, reduce timing)
- [ ] Accessibility: WCAG AA minimum (4.5:1 text contrast)
- [ ] Keyboard navigation: Tab order logical, focus ring visible

---

## 🎨 Non-negotiable Design Decisions

✅ **Teal as primary:** Carries 40-50% of hero section, commands attention
✅ **Solid colors only:** No gradients, per DESIGN.md ban
✅ **Monospace for metrics:** Values are data, deserve distinct treatment
✅ **Conditional coloring:** Badges + progress bars change color based on state (not decorative)
✅ **Animation on load:** Reveals information, guides eye, 300-400ms total
✅ **Hover elevation:** Cards feel interactive, shadow lift on hover
✅ **Metrics answer questions:** Every card = one critical business question
✅ **Context + Alert:** Every value has trend, comparison, or insight
✅ **Mobile-first structure:** Same 4 metrics, same order, adapt to screen
✅ **Accessible by default:** WCAG AA, keyboard nav, respects motion preferences

