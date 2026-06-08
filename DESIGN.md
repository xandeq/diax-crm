# DIAX CRM — Design System

## Color Strategy
**Committed** (one saturated color carries 30-60% of surface, tinted neutrals for supporting elements)

### Palette (OKLCH)
```
Primary (Teal/Cyan): oklch(52% 0.15 189°)  — Dashboard primary, charts, CTAs
Secondary (Blue): oklch(55% 0.13 245°)     — Alerts, positive trends
Accent (Orange): oklch(58% 0.16 35°)       — Revenue metrics, highlights
Success (Green): oklch(65% 0.12 155°)      — Growth, positive indicators
Warning (Amber): oklch(60% 0.14 75°)       — Caution, stalled items
Error (Red): oklch(55% 0.15 25°)           — Negative, losses
```

**Neutrals (tinted toward primary hue):**
```
bg-0: oklch(99% 0.003 189°)    — Page background
bg-1: oklch(96% 0.004 189°)    — Card background
fg-primary: oklch(12% 0.005 189°) — Text primary
fg-secondary: oklch(48% 0.006 189°) — Text secondary
fg-tertiary: oklch(65% 0.005 189°) — Text tertiary / borders
```

### Theme
**Light mode primary, dark-aware fallback**
- Context: Entrepreneur working on MacBook, often morning (café), afternoon (office/home), occasional evening
- Ambient: Mixed (natural + artificial light)
- Decision: Light base (reduced eye strain throughout day), dark mode for evening/focus work

## Typography

### Font Stack
```
Display (headings, hero): "Inter", sans-serif (weight 700, 600)
Body (content, labels): "Inter", sans-serif (weight 400, 500)
Mono (code, numbers in metrics): "Fira Code" or "Menlo", monospace
```

### Scale
```
Display/Hero: 48px (1rem × 3 ratio)
H1: 32px (2.0x body)
H2: 24px (1.5x body)
H3: 18px (1.125x body)
Body: 16px (base)
Label: 12px (0.75x body)
Caption: 11px (0.6875x body)
```

**Line height:** 1.5 (body), 1.2 (headings)
**Letter spacing:** 0 (body), -0.01em (headings 48px+)

## Spacing & Rhythm
```
xs: 4px     (gaps between inline elements)
sm: 8px     (buttons, small padding)
md: 16px    (card padding, section gaps)
lg: 24px    (major section spacing)
xl: 32px    (layout gridlines)
2xl: 48px   (hero top padding)
```

**Grid:** 12-column, 16px gutters

## Components

### Cards
- **Border:** 1px solid oklch(85% 0.004 189°)
- **Radius:** 8px (standard), 12px (hero, emphasized)
- **Shadow:** `0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.04)` (elevated)
- **Background:** oklch(96% 0.004 189°) (light mode)
- **Padding:** 16px (standard), 24px (featured)
- **No nested cards** (anti-pattern)

### Buttons
- **Padding:** 10px 16px (compact), 12px 20px (standard)
- **Border radius:** 6px
- **Font size:** 14px (label), weight 500
- **Primary:** bg-primary (teal), fg-white, no border
- **Secondary:** bg-fg-tertiary (light gray), fg-primary, no border
- **Ghost:** bg-transparent, fg-primary, 1px border
- **States:** hover (brightness +5%), active (brightness -5%), disabled (opacity 50%)
- **No gradients**

### Inputs
- **Border:** 1px solid fg-tertiary
- **Border radius:** 6px
- **Padding:** 10px 12px
- **Focus:** outline 2px solid primary
- **Background:** oklch(99% 0.002 189°)
- **Placeholder:** fg-tertiary

### Charts & Data Viz
- **Color consistency:** Use palette colors in strict order (primary → secondary → accent → etc.)
- **No gradients in bars/areas** (use solid colors)
- **Grid lines:** fg-tertiary at 20% opacity
- **Animation:** 300ms ease-out-quad on load, 150ms ease-out on interaction
- **Tooltip:** bg-fg-primary (text white), rounded 6px, 8px padding

## Motion & Animation

### Easing Curves
```
Entrance: cubic-bezier(0.34, 1.56, 0.64, 1)      — ease-out-back (bouncy, fun)
Exit: cubic-bezier(0.25, 0.46, 0.45, 0.94)       — ease-out-quad (snappy)
Stagger: 30ms between items
```

### Timing
```
Micro (0-100ms): Hover, focus, immediate feedback
Short (100-300ms): Card entrance, tooltip show, value updates
Medium (300-800ms): Page transitions, chart animations
Long (800ms+): Rare (only intro sequences or loading states)
```

### Motion Principles
- **No layout animations** (no `margin`, `width`, `height` changes)
- **Fade + scale** for entrance/exit
- **Color transitions** 150ms
- **Number counters:** Increment smoothly (300ms)
- **Micro-interactions:** Subtle, purposeful (not gratuitous)

## Responsive Design

### Breakpoints
```
Mobile: 0px - 639px (stacked, single column)
Tablet: 640px - 1023px (2-column, adjusted spacing)
Desktop: 1024px+ (full grid, side nav ready)
```

### Mobile-first Strategy
- Base layout: stacked, single column
- 640px: Cards 2 across, navigation shows
- 1024px: Full dashboard grid, hero row, 3+ columns

## Accessibility & Polish

### Color Contrast
- **Minimum:** WCAG AA (4.5:1 for text, 3:1 for graphics)
- **Preferred:** WCAG AAA (7:1 for text)
- **Test:** Check all text + interactive elements

### Focus States
- **Outline:** 2px solid primary, 2px offset
- **Keyboard navigation:** Visible, logical tab order

### Motion Preference
- **Respect `prefers-reduced-motion`:** Scale back animations to 50ms, no easing curves (linear only)

### Loading & Empty States
- **Skeleton loaders:** Pulsing bg-1 blocks, match content shape
- **Empty states:** Illustrated, brief copy, clear next action
- **Errors:** Icon + message, retry button, contact support link

## Implementation Notes

### CSS Architecture
- Tailwind CSS base (shadcn/ui compatible)
- Custom CSS variables for color tokens (fallback)
- Framer Motion for complex animations
- Recharts/Nivo for data visualization

### Component Library
- shadcn/ui (buttons, inputs, cards, dropdowns)
- Lucide React (icons, 24px standard)
- Radix UI (underlying primitives)

### Performance
- Lazy load charts (IntersectionObserver)
- Memoize expensive computations (useMemo)
- Code-split dashboard pages
- Image optimization (next/image)
