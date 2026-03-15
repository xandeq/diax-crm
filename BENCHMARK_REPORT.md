# DIAX CRM Benchmark Report vs Professional Platforms
## Research Completed: March 2026

---

## Executive Summary

This report compares DIAX CRM against four industry-leading platforms: **Pipedrive**, **HubSpot**, **Salesforce**, and **ActiveCampaign**. The research identifies specific features, UX patterns, and architectural approaches that could enhance DIAX's capabilities in email marketing, contact management, lead scoring, behavioral segmentation, analytics, and UI/UX.

**Key Finding:** Professional CRMs emphasize integrated campaign intelligence, multi-signal engagement tracking, and transparent lead scoring. DIAX has solid foundations but lacks sophisticated analytics, visual funnel tracking, and behavior-driven automation.

---

## 1. EMAIL MARKETING & CAMPAIGN MANAGEMENT

### 1.1 Competitive Platform Features

#### **Pipedrive**
- **Campaign Creation & Scheduling:**
  - Personalized email campaigns to unlimited contacts
  - Scheduled drip campaigns with timing optimization
  - Scheduling delay automation (trigger actions at specific times)
  - Contact, deal, and activity data readily available for personalization

- **Personalization & Segmentation:**
  - Dynamic filters: subscription status, bounce reason, send date
  - Workflow automation triggered by data changes
  - Smart triggers and custom sequences
  - Implicit personalization: {{contact_field}} merge fields

- **Template Management:**
  - Pre-built templates with customization
  - Drag-and-drop email editor (implied)
  - Reusable campaign templates

#### **HubSpot**
- **Campaign Builder:**
  - Drag-and-drop email editor with component library
  - Goal-based templates (pre-designed for specific use cases)
  - Device-responsive designs out of the box
  - Campaign grouping and organization

- **Scheduling & Automation:**
  - Email scheduling with optimal send time analysis
  - Workflow automation: trigger actions on opens, clicks, bounces, unsubscribes
  - A/B testing built into the editor (subject lines, content, designs)
  - Dynamic content blocks

- **Reporting:**
  - Open rate, click rate, click-through rate (CTR), click-to-open rate (CTOR)
  - Performance by device type (desktop/mobile)
  - Opens and clicks by email client and device
  - Click map showing which links were clicked and frequency
  - Recipient engagement overview (sent, opens, clicks metrics)

#### **Salesforce (with Pardot/Account Engagement)**
- **Email & Lead Scoring Integration:**
  - Direct integration of email with lead scoring
  - Einstein AI analyzes historical data (demographics, engagement, interactions)
  - Rules-based scoring combined with AI predictive scoring
  - Landing pages and website tracking combined with email data

- **Campaign Management:**
  - Email templates with Salesforce Merge Language (SML)
  - Custom field support in templates
  - Email template attachments (with file size limits)
  - Integration of all channels: email, landing pages, website tracking

#### **ActiveCampaign**
- **Campaign Builder:**
  - Contact segmentation based on behavior and automation entry points
  - Dynamic segmentation: contacts NOT opened/clicked in last 6 months
  - Email sends, opens, clicks tracked in activity windows
  - Automation-based segmentation: track how contacts entered automation

- **AI-Assisted Features:**
  - AI generates 3 behavior-based segments automatically
  - AI identifies patterns in customer behavior
  - Saves time on manual segmentation

### 1.2 DIAX CRM Current Implementation

**What DIAX Has:**
- Three distinct email flows: Funnel Outreach, Campaign Composer, Bulk Send
- Template variable rendering: {{FirstName}}, {{Email}}, {{Company}}
- Background email worker: 50/hour, 250/day limits
- Brevo SMTP integration
- Campaign persistence in database
- Outreach has status tracking: Draft → Scheduled → Sent

**What DIAX Lacks:**
- A/B testing framework for subject lines or content
- Scheduled send time optimization
- Email click map / link tracking
- Open rate and engagement metrics visualization
- Template drag-and-drop editor (relies on plain text)
- Automation workflows (behavior-triggered campaigns)
- Device type performance breakdown
- Email client rendering analytics
- Dynamic content blocks/conditional content

### 1.3 Recommendations for DIAX

#### **High Priority (Implementable, High ROI)**

1. **Email Performance Dashboard**
   - Add to `/email-marketing` and `/campanhas/[id]`:
     - Total sent, open rate, click rate, bounce rate
     - Engagement over time (line chart)
     - Top clicked links (with frequency)
     - Performance by device type
   - Backend: Track email open pixel hits + link click tracking
   - Storage: `EmailQueueItem` → add `Opened`, `OpenedAt`, `ClickedAt`, `DeviceType`, `EmailClient`

2. **Email Template Editor Improvements**
   - Replace plain text with WYSIWYG editor (consider Tiptap integration, already in project)
   - Pre-built template library (cold outreach, follow-up, nurture sequences)
   - Dynamic variable insertion UI: @firstname, @company (auto-complete in editor)
   - Template preview in multiple devices (mobile, desktop)

3. **Campaign Analytics Page**
   - Dedicated route: `/campanhas/[id]/analytics`
   - Metrics: sent count, open count, click count, conversion (if linked to CRM outcomes)
   - Funnel view: Sent → Opened → Clicked → CRM Action (e.g., lead status change)
   - Time series: opens/clicks by hour (heatmap for best send times)

#### **Medium Priority (Moderate ROI, Doable)**

4. **Automation Workflows**
   - **If/Then Logic:** Create automation rules in UI
     - Trigger: "Email opened" → Action: "Mark lead as Hot segment"
     - Trigger: "Link clicked" → Action: "Create task: Follow up"
     - Trigger: "Email bounced" → Action: "Mark contact for cleanup"
   - Backend: Create `AutomationRule` and `AutomationTrigger` entities
   - Worker: Check rules after each email event

5. **Dynamic Content & Personalization**
   - Allow template to include conditional blocks: `[IF company=TechCorp]...[ELSE]...[/IF]`
   - Surface merge field picker in campaign composer UI
   - Show contact field options: Name, Email, Company, Status, Segment, Tags

6. **A/B Testing Framework**
   - Campaign composer: "Create variant" button to split test
   - Test on: subject line, content, send time
   - Track: open rate, click rate per variant
   - Winner selection: manual (user picks) or automatic (highest engagement after 24h)
   - Store variants: `EmailCampaign` → add `IsControl` flag + `VariantGroupId`

#### **Nice-to-Have (Longer-term, Lower ROI)**

7. **AI-Powered Send Time Optimization**
   - Store open times per contact
   - Recommend optimal send window for individual contact
   - Use historical open rates to suggest send time for batch campaigns

8. **Email Rendering Tests**
   - On campaign create, test render in top email clients (Gmail, Outlook, Apple Mail, etc.)
   - Show preview grid: client × device
   - Flag CSS/design issues before send

---

## 2. CONTACT/LEAD MANAGEMENT & TIMELINE

### 2.1 Competitive Platform Features

#### **HubSpot**
- **Contact Record Timeline:**
  - Chronological feed of: emails, calls, tasks, meetings, form submissions, page views
  - Custom event tracking (user-defined activities)
  - Intent signals as timeline events (keyword mentions, content downloads)
  - Filterable by activity type (Notes, Emails, Calls, Tasks, Meetings)
  - Last engagement date property (tracks last 1-1 email, form submit, meeting book, lead revisit)

- **Engagement Scoring:**
  - AI-powered engagement score creation
  - Initial setup: intuitive dropdowns, easily weighted properties
  - Properties tracked: email opens, clicks, form submissions, page views, meeting attendance
  - Transparent scoring: each property shows its weight/contribution

- **Contact Fields:**
  - ~100+ default properties (name, email, phone, company, job title, lifecycle stage, etc.)
  - Custom properties and field groups
  - Formulas and rollup fields for computed values

#### **Pipedrive**
- **Contact & Organization Records:**
  - Divides contacts into "people" and "organizations"
  - Link people to deals
  - Visual history of all calls, emails, activities with contacts
  - Check conversation history before follow-up

- **Activity Tracking:**
  - Email conversations (two-way sync via BCC or Inbox)
  - Phone calls with call recording integration
  - Meeting scheduling (Google/Microsoft calendar sync)
  - Visual timeline in contact/deal records
  - Activity filtering by type

#### **Salesforce**
- **Contact Record Customization:**
  - 10 fields max in compact layouts (for mobile)
  - Custom field creation per requirement
  - Record page customization via Lightning App Builder
  - Dynamic Forms: migrate fields into individual components, move freely
  - 1-N relationship fields (contact → account, contact → opportunities)

- **Timeline Integration:**
  - Activity history per contact
  - Email/call logs (if integrated)
  - Related records (accounts, opportunities, activities)

#### **ActiveCampaign**
- **Contact Activity Tracking:**
  - Activity windows: emails, opens, clicks, date ranges
  - Automation entry point tracking (how contact entered automation)
  - Behavior-based segmentation (no form submissions in 6 months, etc.)
  - Custom fields per contact

### 2.2 DIAX CRM Current Implementation

**What DIAX Has:**
- `Customer` entity: Name, Email, Phone, WhatsApp, CompanyName, Website, Notes, Tags
- Status (0-4), Segment (Cold/Warm/Hot), Source (Manual/Scraping/Import/GoogleMaps)
- Audit logging: tracks field changes over time
- Email sent count and last email sent at timestamps
- Relationship: Customers ↔ LeadTags (many-to-many)

**What DIAX Lacks:**
- No contact activity timeline visualization
- No chronological event history (emails, calls, notes, tasks)
- No engagement scoring system
- No custom field framework
- No form submission tracking
- No website visit tracking
- Limited activity event types (email only, not calls/meetings/notes)
- No rich contact property management
- No relationship mapping (who is the decision maker? linked contacts?)

### 2.3 Recommendations for DIAX

#### **High Priority**

1. **Contact Activity Timeline Component**
   - New component: `src/components/ContactTimeline.tsx`
   - Display chronological list of: emails (with open/click status), notes, status changes, segment changes, tag additions
   - Filter by activity type: All, Emails, Notes, Status Changes, Segment Changes
   - Order: newest first
   - Backend: Create `ContactActivityLog` table or use audit logs with filtering
   - Each activity card: timestamp, action type, details, actor (system/user)

   **Database Schema:**
   ```
   ContactActivity (Id, ContactId, ActivityType, ActivityDate, Details, CreatedAt, CreatedBy)
   ActivityType: Email, Note, StatusChange, SegmentChange, TagAdded, TagRemoved, Created
   ```

2. **Rich Notes/Comments System**
   - Add to customer record: notes section with rich text (Tiptap, already used in project)
   - Each note: text, author, timestamp
   - Link notes to email campaigns (e.g., "Follow-up note after campaign X")
   - Show notes in timeline
   - Backend: `ContactNote` entity (Id, ContactId, Content, CreatedAt, CreatedBy)

3. **Last Engagement Timestamp**
   - Track: last email opened, last email clicked, last note added, last status change
   - Display prominently on contact record header
   - Use in segmentation: "Contacts with no engagement in 30 days"
   - Already partially tracked (LastEmailSentAt) — extend to include opens/clicks

#### **Medium Priority**

4. **Basic Engagement Scoring**
   - Simple rule-based scoring (not AI yet):
     - Email sent: +1 point
     - Email opened: +3 points
     - Email clicked: +5 points
     - Status upgraded: +10 points
     - Status reached "Customer": +50 points
   - Recalculate on email events and manual status changes
   - Display score on contact card (e.g., "Engagement: 23 points")
   - Sort contacts by engagement score
   - Use score in segment filtering ("Hot" = score > 20)

5. **Custom Fields Framework**
   - Admin page: `/admin/custom-fields/` to manage
   - Field types: Text, Number, Date, Select, Checkbox
   - Allow admin to add fields per entity (just Customer for v1)
   - Store dynamically: `CustomFieldValue` table (ContactId, FieldDefinitionId, Value)
   - Surface in contact form and timeline

6. **Email Event Webhooks from Brevo**
   - Already receive: delivered, opened, bounce, spam, unsubscribed
   - **Implement:** Create `ContactActivityLog` entries for each event
   - Timeline will automatically show: "Email opened at 2026-03-14 10:30 AM"
   - Link to specific email campaign

#### **Nice-to-Have (Longer-term)**

7. **Decision-Maker Relationship Mapping**
   - Add `PrimaryContactId` to Customer (for organization records)
   - Show related contacts on organization record
   - Track influence chain: who referred? who is decision-maker?

8. **Website Visit Tracking**
   - Pixel-based tracking (similar to email opens)
   - Log: page URL, visit date, session duration
   - Show in timeline: "Visited pricing page"
   - Used in engagement scoring

---

## 3. BEHAVIORAL SEGMENTATION & REMARKETING

### 3.1 Competitive Platform Features

#### **ActiveCampaign**
- **Dynamic Behavioral Segmentation:**
  - Event tracking: user actions on website/email tracked automatically
  - Segment conditions: "Contacts who have NOT opened/clicked emails in last 6 months"
  - Automation-based: contacts segmented by how they entered automation
  - Activity windows: use dates based on email sends, opens, clicks
  - Time-based rules: e.g., "opened in last 30 days", "clicked within 7 days"

- **Automated Segmentation:**
  - AI generates 3 segments automatically based on behavior
  - Segments re-evaluated continuously (dynamic)
  - Contacts can enter/exit segments based on rule changes
  - Use segments in: automations, campaigns, reporting

- **Use Cases:**
  - Clean-up: "Not opened any email in 6 months" → unsubscribe automation
  - Re-engagement: "Engaged in past 30 days" → premium content
  - Nurture: "Downloaded whitepaper" → educational drip sequence

#### **Pipedrive**
- **Lead Scoring & Segmentation:**
  - Assign points to actions: email opens, website visits, form fills
  - Rules-based: pre-defined point values per action
  - Automation triggers: tag leads as "MQL" when score > threshold
  - Workflows: on score change, trigger notifications or status changes

- **Personalization at Scale:**
  - Filter contacts: subscription status, bounce reason, send date
  - Smart personalization: trigger at right funnel point
  - Custom sequences per segment

#### **HubSpot**
- **List & Workflow Segmentation:**
  - Create lists based on: properties, activities, engagement, custom events
  - Workflow triggers: contact property change, form submission, email interaction
  - Dynamic lists: re-evaluated as contacts change properties
  - Activity-based: "Opened email in last 7 days"

- **Intent Signals:**
  - Third-party intent data: keyword mentions, content downloads (intent signals)
  - Use in: workflows, list creation, lead scoring
  - Timeline shows intent signals as events

#### **Salesforce Einstein**
- **AI-Powered Lead Scoring:**
  - Predictive model: analyzes historical leads to find patterns
  - Factors: demographics, engagement, interactions, firmographics
  - Top Positive/Negative Predictive Factors shown (transparency)
  - Dynamic scoring: re-evaluated continuously

### 3.2 DIAX CRM Current Implementation

**What DIAX Has:**
- `Segment` enum: Cold (0), Warm (1), Hot (2)
- Funnel Outreach auto-segments leads: Hot/Warm/Cold based on...? (implementation unclear)
- Tags: many-to-many with Customer, comma-separated in UI
- Email segmentation: can filter contacts before sending
- Lead status: 0-4 (Lead, Contacted, Qualified, Negotiating, Customer)

**What DIAX Lacks:**
- No automated segmentation rules
- No behavior-triggered workflows
- No time-based conditions ("not engaged in X days")
- No activity-based segmentation (e.g., "opened email in last 7 days")
- No dynamic segment re-evaluation
- No AI/rules engine for segmentation
- No segment-triggered email campaigns (automated remarketing)
- No intent signal tracking (external data sources)

### 3.3 Recommendations for DIAX

#### **High Priority**

1. **Behavioral Segmentation Rules Engine**
   - Backend: Create `SegmentationRule` and `SegmentationCondition` entities
   - Rule: "Re-engagement Campaign Targets"
   - Conditions:
     - Status IN [Lead, Contacted, Qualified]
     - LastEmailOpenedAt < 30 days ago
     - EmailOptOut = false
   - Re-evaluate rules: daily batch job or on-demand
   - Store results: `ContactSegmentMembership` (ContactId, RuleId, EvaluatedAt)

   **Admin UI:** `/admin/segmentation-rules/`
   - Rule builder: visual "AND/OR" logic
   - Condition picker: LastEmailOpenedAt, LastEmailClickedAt, Status, Segment, Tag, CreatedAt, etc.
   - Preview: "This rule matches X contacts"
   - Test: execute rule, show matching contacts

2. **Tag-Based Segmentation**
   - Expand tags: currently comma-separated strings
   - Model: `LeadTag` entity with Id and Name
   - UI: searchable tag picker on contact record (like GitHub labels)
   - Segment by tag: "All contacts tagged 'VIP'" → send premium content
   - Auto-tagging: campaign composer can tag recipients after send
   - Tag filters in list views: multi-select filter in contacts table

3. **Time-Based Segmentation Conditions**
   - Rule conditions support: "X days ago", "X days from now"
   - Examples:
     - "Email opened more than 30 days ago"
     - "Created less than 7 days ago" (new leads)
     - "Status changed to Customer within last 60 days"
   - Backend: calculate date differences in rule evaluation

#### **Medium Priority**

4. **Automation Workflows for Remarketing**
   - Simple workflow builder: Trigger → Condition → Action
   - Trigger: "Contact matches segmentation rule X"
   - Condition: "AND not emailed in last 7 days"
   - Action: "Send email campaign Y"
   - Backend: `AutomationWorkflow`, `WorkflowTrigger`, `WorkflowAction`
   - Worker: check triggers every N hours, queue matching contacts for action

   **Example:**
   ```
   Name: "Re-engage Cold Leads"
   Trigger: Daily at 9 AM
   Condition: Segment = Cold AND LastEmailOpenedAt > 60 days
   Action: Send "Check-in" campaign
   Repeat: Weekly for 4 weeks
   ```

5. **Segment-Based Email Campaign Targeting**
   - Campaign composer: select segment instead of individual contacts
   - "Apply segmentation rule" dropdown
   - Dynamic count: "This campaign will reach X contacts (evaluated at send time)"
   - Send workflow: evaluate rule → queue matching contacts → send

#### **Nice-to-Have**

6. **Predictive Engagement Scoring (Future)**
   - Use historical engagement data to predict lead quality
   - ML model: "Contacts similar to customers are more likely to convert"
   - Feature importance: which actions drive conversions?

7. **Intent Signal Integration**
   - Integrate with external intent data (keyword mentions, form fills across web)
   - Import signals: contact visited "pricing" page → mark intent
   - Use in scoring and segmentation

---

## 4. LEAD SCORING & QUALIFICATION

### 4.1 Competitive Platform Features

#### **Salesforce Einstein Lead Scoring**
- **Methodology:**
  - Machine learning analyzes historical lead data
  - Identifies patterns in converted vs unconverted leads
  - Considers: demographics (company size, industry), engagement (email opens, form fills), past interactions
  - Continuous learning: improves as more data accumulates

- **Output:**
  - Lead score: 0-100 (or custom scale)
  - Top Positive Predictive Factors: "Email clicks" +15%, "Company size: Enterprise" +12%
  - Top Negative Predictive Factors: "No engagement for 6 months" -20%
  - Transparent: users see why a lead scored high/low

#### **Pipedrive**
- **MQL/SQL Framework:**
  - MQL (Marketing Qualified Lead): shows intent (form fill, webinar, content download), fits persona
  - SQL (Sales Qualified Lead): vetted, ready for sales outreach, demonstrated buying intent
  - SAL (Sales Accepted Lead): sales team accepted the lead (handoff complete)

- **Scoring Rules:**
  - Explicit: assign points to actions (form fill = 5, email open = 1, demo request = 20)
  - Custom workflows: trigger on score threshold (e.g., score > 50 → tag as MQL → notify sales)
  - Automation: tag, move to pipeline, trigger notifications

- **Best Practices (from research):**
  - Define based on: demographics (industry, company size, job title), activities (opens, clicks, form fills), behaviors (website visits, content downloads)
  - Compare characteristics of best customers and incorporate into lead definition
  - Automate MQL→SQL handoff with clear criteria

#### **HubSpot**
- **AI-Powered Engagement Scores:**
  - "Engagement score": based on weighted properties
  - Initial setup: drop-down interface, easy property selection
  - Weights: email opens (3 pts), clicks (5 pts), form submission (10 pts), meeting booked (20 pts)
  - Transparent: each property shows its contribution

- **Intent Signals:**
  - Third-party intent data: keyword mentions, content downloads
  - Use in lead scoring: high intent = higher score
  - Trigger workflows: on high intent, escalate to sales

#### **ActiveCampaign**
- **Behavioral Scoring (via Automation):**
  - Contacts can be tagged/scored based on automation entry
  - Segments show behavior patterns
  - AI identifies 3 key segments automatically

### 4.2 DIAX CRM Current Implementation

**What DIAX Has:**
- `Segment` enum: Cold, Warm, Hot (manual classification only)
- `Status` enum: Lead → Contacted → Qualified → Negotiating → Customer (pipeline stages)
- No explicit lead scoring system
- No MQL/SQL distinction
- Manual segmentation (admin sets segment)

**What DIAX Lacks:**
- No lead scoring algorithm (explicit or AI)
- No points-based scoring (no actions tracked with points)
- No MQL identification (which leads are marketing-ready?)
- No SQL identification (which leads are sales-ready?)
- No transparent scoring factors ("why is this lead Hot?")
- No score-based automation triggers
- No engagement signals combined with demographics
- No historical analysis (patterns in converted vs lost leads)

### 4.3 Recommendations for DIAX

#### **High Priority**

1. **Explicit Lead Scoring System**
   - Admin page: `/admin/lead-scoring-rules/`
   - Define scoring rules per action:
     - Email sent: 0 pts (just tracking)
     - Email opened: 1 pt
     - Email clicked: 3 pts
     - Status upgraded (Contacted): 5 pts
     - Status upgraded (Qualified): 10 pts
     - Status upgraded (Negotiating): 15 pts
     - Created from lead import: 2 pts
     - Contact created: 1 pt

   - Backend: `LeadScoringRule` (RuleId, Action, Points, Description)
   - Calculate: sum all applicable points for contact
   - Store on contact: `EngagementScore` (int) field
   - Recalculate on: email events, status changes, creation

   **Formula (simplified):**
   ```csharp
   Score = (EmailOpenCount * 1) + (EmailClickCount * 3) + (StatusLevel * 5) + ...
   ```

2. **Lead Qualification Mapping**
   - Define thresholds:
     - MQL (Marketing Qualified Lead): Score >= 5 AND Status >= Contacted
     - SQL (Sales Qualified Lead): Score >= 15 AND Status >= Qualified
     - Hot Lead: Score >= 20

   - Add properties: `IsMarketingQualified`, `IsSalesQualified`, `IsHot` (computed)
   - Use in filtering: show "SQLs" in focus list
   - Backend: evaluate on score change, surface in API response

3. **Score Transparency**
   - Contact record: show score breakdown
     - Email Opens: 5 pts
     - Email Clicks: 12 pts
     - Status: 10 pts
     - **Total: 27 pts**
   - Show "SQL Threshold: 15" so user understands why lead qualifies
   - Timeline: "Score increased by 3 pts (email click)" for each change

#### **Medium Priority**

4. **Demographic Scoring Factors**
   - Add company-level scoring:
     - Company size: Enterprise = 5 pts, SMB = 2 pts, Startup = 0 pts
     - Industry: match to ICP (Ideal Customer Profile) — Tech = 3 pts, Finance = 5 pts, etc.
   - Combine with engagement: Score = Engagement (70%) + Fit (30%)
   - Requires: ICP definition in admin, company size/industry data on contacts

5. **Score-Based Automation**
   - Workflow: when score crosses threshold, trigger action
     - Score >= 15 → tag as "SQL" + create task "Call lead"
     - Score >= 20 → move to "Hot segment"
     - Score drops < 5 → move to "Cold segment"
   - Notification: admin gets alert "3 new SQLs" daily

6. **Lead Performance Analytics**
   - Dashboard: `/analytics/lead-scoring/`
   - Charts:
     - Score distribution (histogram): how many leads in each bucket
     - Score vs outcome (scatter): leads converted vs not, by score
     - Top scoring actions: which actions drive highest scores
     - MQL → SQL conversion rate
     - SQL → Customer conversion rate

#### **Nice-to-Have**

7. **Predictive Lead Scoring (Future)**
   - Historical analysis: which leads converted vs churned?
   - Extract features: engagement history, source, company info
   - Train ML model to predict conversion probability
   - Salesforce Einstein, HubSpot do this with AI
   - For DIAX: could use scikit-learn or external API (e.g., Clearbit enrichment)

8. **Dynamic Score Decay**
   - Older engagements worth less: email opened 6 months ago = 0.5 pts
   - Keeps leads fresh: stale leads naturally drop in score
   - Encourages re-engagement campaigns

---

## 5. CAMPAIGN ANALYTICS & REPORTING

### 5.1 Competitive Platform Features

#### **HubSpot**
- **Email Campaign Metrics:**
  - Sent, Open Rate, Click Rate, Click-Through Rate (CTR)
  - Click-to-Open Rate (CTOR): % of openers who clicked (shows content quality)
  - Performance by device: opens/clicks on desktop vs mobile
  - Performance by email client: Gmail, Outlook, Apple Mail, etc.
  - Click map: visualization of which links were clicked and frequency

- **Multi-Channel Attribution (Optional Reports):**
  - Custom reporting: choose time-based metrics
  - Single object reports: filter by campaign, date range, properties
  - Compare results across campaigns (A/B test winner analysis)

#### **Salesforce**
- **Campaign Analytics:**
  - Combine sales and marketing data: track campaign impact on sales
  - Multi-channel integration: email + landing pages + website + CRM
  - Revenue attribution: tie campaign to closed deals
  - ROI reporting: cost per lead, cost per opportunity

- **Einstein Analytics (Optional):**
  - Predictive forecasting: expected revenue from current pipeline
  - Opportunity health: likelihood of close based on signals

#### **Pipedrive**
- **Pipeline Marketing:**
  - Visual sales pipeline: kanban boards with drag-drop
  - Track deals through stages
  - Link campaigns to deals: see campaign impact on revenue

- **Email Performance:**
  - Implied: open rates, click rates (from campaigns)
  - Funnel view: leads → opportunities → deals

#### **General Best Practices (from research)**
- **Key Dashboard Metrics:**
  - Lead source performance: which sources produce best leads?
  - Lead quality scores: average score by source
  - Velocity: time from lead to MQL to SQL
  - Conversion rates: stage-to-stage (lead → qualified → customer)
  - Cost per lead, Cost per acquisition (if integrated with budgets)
  - ROI/Attribution: revenue influenced by campaign

- **Lead Generation Funnel:**
  - Visualization: 1000 leads → 200 qualified → 50 negotiating → 10 customers
  - Shows attrition and conversion rates at each stage
  - Helps set targets

### 5.2 DIAX CRM Current Implementation

**What DIAX Has:**
- Email campaign persistence: campaigns stored in DB
- Email queue tracking: sent/processing/failed status per item
- Brevo webhook integration: open, click, bounce tracking (partial)
- Campanhas module: `/campanhas/[id]/` (exists but limited details)
- Basic email counts: `EmailSentCount` on contact

**What DIAX Lacks:**
- No email engagement metrics dashboard (open rate, click rate, etc.)
- No device/client performance breakdown
- No click map visualization
- No campaign ROI/attribution (emails → lead status changes → revenue?)
- No lead source performance analysis
- No lead quality metrics by source
- No funnel visualization (leads → qualified → customers)
- No multi-touch attribution
- No cohort analysis (leads sent email X date → conversion rate)
- No A/B test performance comparison
- No predictive analytics (forecast revenue, close probability)

### 5.3 Recommendations for DIAX

#### **High Priority**

1. **Email Campaign Analytics Dashboard**
   - Route: `/campanhas/[id]/analytics`
   - Metrics displayed:
     - **Overview cards:**
       - Total sent
       - Open count + rate (%)
       - Click count + click rate (%)
       - Bounce count
       - Unsubscribe count

     - **Engagement chart (line):**
       - X-axis: time (hours since send)
       - Y-axis: cumulative opens/clicks
       - Show engagement curve

     - **Top clicked links (table):**
       - Link URL, Click count, % of total clicks

     - **Device performance (bar chart):**
       - Desktop opens vs Mobile opens

     - **Email client breakdown (pie or bar):**
       - Gmail, Outlook, Apple Mail, Other

   - Backend: aggregate `EmailQueueItem` data (status, metrics)
   - Frontend: chart library (already using shadcn, could use Recharts)

2. **Email Engagement Tracking Enhancement**
   - Brevo webhooks already send open/click events
   - Implement: store in `EmailQueueItem`:
     - `Opened` (bit), `OpenedAt` (datetime)
     - `Clicked` (bit), `ClickedAt` (datetime)
     - `ClickedLinks` (JSON): [{ url, count, timestamp }]
     - `DeviceType` (string): from Brevo "UserAgent"
     - `EmailClient` (string): inferred from UserAgent
   - Migration: new columns in `EmailQueueItem` table

3. **Lead Source Performance Report**
   - Route: `/analytics/lead-sources/`
   - Metrics by source (Manual, Scraping, Import, GoogleMaps):
     - Total leads by source
     - Average engagement score by source
     - Conversion rate: source → Customer (% of source leads that became customers)
     - Time to conversion (average days from created to status=Customer)
     - Lead quality: avg score, % MQL, % SQL

   - Visualization:
     - Bar chart: lead count by source
     - Table: source, count, quality score, conversion rate

   - Database: already have `Source` enum on `Customer`

4. **Lead Funnel Visualization**
   - Route: `/analytics/funnel/`
   - Stages: Lead → Contacted → Qualified → Negotiating → Customer
   - Show:
     - Count at each stage
     - Conversion rate: stage-to-next (e.g., 40% of Leads become Contacted)
     - Attrition rate
     - Time in stage (avg days)

   - Visualization: funnel chart (wide at top, narrows at bottom)
   - Filter by: date range, source, segment, tag

   - Example:
     ```
     Lead (1000) → 40%
     Contacted (400) → 50%
     Qualified (200) → 50%
     Negotiating (100) → 70%
     Customer (70)
     ```

#### **Medium Priority**

5. **Campaign Comparison & A/B Test Analysis**
   - Route: `/analytics/campaign-comparison/`
   - Compare 2+ campaigns side-by-side:
     - Campaign name, send date, recipient count
     - Open rate, click rate, bounce rate
     - Conversion (if linked to CRM outcomes)
   - Identify winner: highest open rate? click rate?
   - Time to peak: which campaign peaked fastest?

6. **Lead Cohort Analysis**
   - Track cohorts: "Leads created in Week of March 3"
   - Show cohort progression over time:
     - Week 0: 150 leads created
     - Week 1: 90 became Contacted (60%), 50 became Qualified
     - Week 2: 40 became Qualified, 20 became Negotiating
     - Week 4: 8 became Customers (5% conversion)
   - Useful for: understanding sales velocity, comparing source quality

7. **Multi-Touch Attribution (Basic)**
   - Track customer journey: which touchpoints led to conversion?
   - Attribute conversion to: campaign, source, interactions
   - Question: "Of our 70 customers, how many came from email marketing?"
   - Model: first-touch, last-touch, linear (equal credit)
   - Backend: create `ContactJourney` audit log, trace path to customer

#### **Nice-to-Have**

8. **Predictive Insights**
   - Forecast: expected customers from current leads
   - Churn prediction: leads likely to churn based on engagement
   - Recommendations: "Re-engage this segment, they're disengaging"

---

## 6. USER EXPERIENCE & NAVIGATION PATTERNS

### 6.1 Competitive Platform UX Patterns

#### **Pipedrive**
- **Navigation:**
  - Sidebar with main modules: Deals, Contacts, Companies, Campaigns, etc.
  - Pipeline view (kanban board) as primary interface for deals
  - Contact/company records in modal or side panel
  - Email integration in contact record

- **Interaction Model:**
  - Drag-and-drop deal movement
  - Quick actions on contacts (call, email, send campaign)
  - Activity feed inline on records
  - Timeline visualization of communications

#### **HubSpot**
- **Navigation:**
  - Top nav with modules: CRM, Marketing, Sales, Service
  - Left sidebar within each module
  - Contact record: primary canvas with activity feed
  - Campaigns: separate module for email management

- **Layout:**
  - Contact details: main panel
  - Activity timeline: scrollable feed below
  - Properties editor: right sidebar
  - Related records: tabs (deals, companies, etc.)

#### **Salesforce**
- **Navigation:**
  - Lightning top nav
  - Sidebar (collapsible) with module navigation
  - App switcher (top-left)
  - Search (global, for records/reports)

- **Record Layout:**
  - Customizable: admin defines field grouping
  - Compact layout on mobile
  - Dynamic forms: fields grouped and reordered
  - Related lists: linked records in tabs

#### **ActiveCampaign**
- **Navigation:**
  - Top nav: Contacts, Campaigns, Automations, Reports, Admin
  - Contacts as central hub
  - Inline contact editing
  - Automations as visual workflows

- **Visual Design:**
  - Simple, minimalist
  - Color-coded statuses
  - Icon-rich UI

### 6.2 DIAX CRM Current Implementation

**What DIAX Has:**
- Header navigation: dropdown menus (Financeiro, Clientes, Ferramentas, etc.)
- No sidebar: header-only navigation
- Module structure: `/leads`, `/customers`, `/email-marketing`, `/campanhas`, `/finance/`, `/tools/`
- List-based interface: tables with data
- Modal dialogs: campaign composer, import
- Contact record: likely a page/modal view

**Navigation Structure (inferred):**
```
Header
  ├─ Financeiro
  │  ├─ Contas (Accounts)
  │  ├─ Receita (Income)
  │  ├─ Despesa (Expense)
  │  └─ Planejador (Planner)
  ├─ Clientes
  │  ├─ Leads
  │  ├─ Customers
  │  └─ Import
  ├─ Ferramentas
  │  ├─ Email Marketing
  │  ├─ Outreach
  │  ├─ AI Tools
  │  └─ Image Generation
  ├─ Campanhas (Campaigns)
  └─ Admin
```

**What DIAX Lacks:**
- No sidebar: limits quick access to modules
- No contact record detail page (likely exits in views but not prominent)
- No timeline/activity feed visualization on contact records
- No kanban/pipeline view for leads (visual progression)
- No quick-action buttons (email contact, call, assign task)
- No related-records tabs (campaigns contact is in, recent emails, etc.)
- No search/global search (hard to find leads)
- No drag-and-drop (segment movement, deal stage, etc.)
- Limited mobile responsiveness

### 6.3 Recommendations for DIAX

#### **High Priority**

1. **Contact Record Detail Page**
   - Route: `/customers/[id]` (already exists?)
   - Layout (2-column):
     - **Left (70%):** Main panel
       - Header: Contact name, company, email, phone, status badge
       - Quick actions: Send email, Create task, Add note, Change status/segment
       - Contact details: editable form (Name, Email, Phone, Company, etc.)
       - Activity timeline: emails, notes, status changes

     - **Right (30%):** Sidebar
       - Engagement score (if implemented)
       - Segment badge
       - Source badge
       - Tags (clickable)
       - Recent campaigns (list of campaigns contact was in)
       - Quick info: LastEmailSentAt, Created date

   - Interactions:
     - Click "Send Email" → modal campaign composer pre-filled with contact
     - Click status → dropdown to change
     - Click segment → dropdown to change
     - Add note → inline editor

2. **Improved Header Navigation (Collapsible Sidebar)**
   - Add subtle sidebar: `/` to toggle
   - Sidebar shows:
     - Quick-access modules (Leads, Customers, Campaigns, Email)
     - Pinned contacts (favorites)
     - Quick search (name, email)
   - Keeps header clean, allows quick navigation
   - Option: collapse on mobile

3. **Contact List View Enhancements**
   - Add columns: Engagement Score, Last Email, Segment, Status, Source
   - Filter row: search, segment filter, status filter, date range
   - Sort: by engagement score, last email, status
   - Bulk actions: select multiple → Send email, Change segment, Add tag
   - Quick view: click row → side panel (don't navigate away)

#### **Medium Priority**

4. **Contact Record Tabs**
   - Tab structure on contact detail:
     - **Overview:** basic info, form
     - **Activity:** timeline (emails, notes, status changes)
     - **Campaigns:** table of campaigns contact is in + engagement metrics
     - **Email History:** list of all emails sent to contact + open/click status

5. **Lead Pipeline Kanban View**
   - Route: `/leads/pipeline/` (alternative to table view)
   - Kanban board: columns for each status (Lead, Contacted, Qualified, Negotiating, Customer)
   - Cards: contact name, company, engagement score
   - Drag-drop: move contact between stages
   - Grouping: by segment (tabs: All, Hot, Warm, Cold)
   - Quick actions: right-click contact → email, note, etc.

6. **Global Search**
   - Header: search box that searches across:
     - Contact names, email addresses
     - Campaign names
     - Companies
   - Keyboard shortcut: Cmd+K or Ctrl+K
   - Results: grouped by type (Contacts, Campaigns, Companies)
   - Quick navigation: select result → jump to detail

#### **Nice-to-Have**

7. **Mobile-Responsive Design**
   - Contact list: hide columns on mobile, show card view
   - Contact detail: stack 2-column into 1-column
   - Sidebar: hamburger menu on mobile
   - Timeline: adapted for small screens

8. **Dark Mode**
   - Toggle in admin preferences
   - Use Tailwind dark: classes
   - Accessible color contrast

---

## 7. SUMMARY TABLE: Feature Gap Analysis

| Feature | Pipedrive | HubSpot | Salesforce | ActiveCampaign | DIAX | Priority |
|---------|-----------|---------|-----------|----------------|------|----------|
| **Email Campaigns** | ✓ | ✓ | ✓ (Pardot) | ✓ | ✓ (basic) | Medium |
| Email A/B Testing | ✓ | ✓ | ○ | ○ | ✗ | Medium |
| Email Open/Click Tracking | ✓ | ✓ | ✓ | ✓ | ✓ (via Brevo) | High |
| Click Map Visualization | ✓ | ✓ | ○ | ○ | ✗ | Medium |
| Drag-Drop Email Editor | ✓ | ✓ | ✓ | ○ | ✗ | Medium |
| **Contact Timeline** | ✓ | ✓ | ✓ | ✓ | ✗ | **High** |
| Activity Timeline | ✓ | ✓ | ✓ | ✓ | ✗ | **High** |
| Engagement Score (AI) | ○ | ✓ | ✓ | ○ | ✗ | **High** |
| Custom Fields | ✓ | ✓ | ✓ | ✓ | ✗ | Medium |
| **Behavioral Segmentation** | ✓ | ✓ | ○ | ✓ | ✗ | **High** |
| Dynamic Segments | ✓ | ✓ | ○ | ✓ | ✗ | **High** |
| Automation Workflows | ✓ | ✓ | ✓ | ✓ | ✗ | **High** |
| **Lead Scoring** | ✓ | ✓ | ✓ (Einstein) | ✓ | ✗ | **High** |
| Explicit/Rule-Based Scoring | ✓ | ○ | ✓ | ○ | ✗ | **High** |
| MQL/SQL Identification | ✓ | ○ | ✓ | ○ | ✗ | High |
| Scoring Transparency | ✓ | ✓ | ✓ | ○ | ✗ | Medium |
| **Analytics & Reporting** | ✓ | ✓ | ✓ | ✓ | ✗ | **High** |
| Email Performance Dashboard | ✓ | ✓ | ✓ | ✓ | ✗ | **High** |
| Campaign Comparison | ✓ | ✓ | ✓ | ✓ | ✗ | Medium |
| Lead Funnel Visualization | ✓ | ✓ | ✓ | ✓ | ✗ | **High** |
| Lead Source Performance | ✓ | ✓ | ✓ | ✓ | ✗ | **High** |
| Multi-Touch Attribution | ✓ | ✓ | ✓ | ○ | ✗ | Medium |
| **UI/UX & Navigation** | ✓ | ✓ | ✓ | ✓ | ✗ | **High** |
| Contact Record Detail Page | ✓ | ✓ | ✓ | ✓ | ✗ | **High** |
| Pipeline Kanban View | ✓ | ✓ | ✓ | ○ | ✗ | Medium |
| Global Search | ✓ | ✓ | ✓ | ✓ | ✗ | Medium |
| Sidebar Navigation | ✓ | ✓ | ✓ | ✓ | ✗ (header-only) | Low |
| Mobile Responsive | ✓ | ✓ | ✓ | ✓ | ✗ | Low |

**Legend:** ✓ = Full support, ○ = Partial support, ✗ = Not implemented

---

## 8. IMPLEMENTATION ROADMAP (Phased)

### **Phase 1: Foundation (Weeks 1-2) — Contact Timeline & Scoring**
Priority: Get visibility into lead engagement

1. **Contact Activity Timeline** (High ROI)
   - Add `ContactActivityLog` table
   - Create `ContactTimeline.tsx` component
   - Display in contact detail page
   - Track: emails, notes, status changes

2. **Lead Scoring v1** (Explicit Rules)
   - Define scoring rules in admin
   - Recalculate on events
   - Display score on contact card
   - Filter/sort by score

3. **Contact Detail Page**
   - Build contact record layout
   - Embed timeline component
   - Show score, segment, source

**Effort:** 2-3 weeks, 1 dev, High impact

---

### **Phase 2: Analytics (Weeks 3-4) — Campaign Reporting**
Priority: Understand campaign performance

1. **Email Campaign Analytics Dashboard**
   - `/campanhas/[id]/analytics`
   - Track opens, clicks, bounces
   - Device/client breakdown
   - Top links clicked

2. **Lead Funnel Visualization**
   - `/analytics/funnel/`
   - Stage-to-stage conversion
   - Time in stage analysis

3. **Lead Source Performance**
   - By source: count, quality, conversion rate
   - Time to conversion

**Effort:** 2 weeks, 1 dev, High impact

---

### **Phase 3: Segmentation & Automation (Weeks 5-6)**
Priority: Enable targeted remarketing campaigns

1. **Behavioral Segmentation Rules**
   - Rule builder UI
   - Time-based conditions
   - Automated rule evaluation

2. **Automation Workflows**
   - Trigger → Condition → Action
   - Segment-based targeting

3. **Tag Management**
   - Rich tag system (not just strings)
   - Tag filtering, auto-tagging

**Effort:** 2-3 weeks, 1 dev, High impact

---

### **Phase 4: Email Enhancements (Weeks 7-8)**
Priority: Improve email creation and testing

1. **Email Template Editor**
   - WYSIWYG editor (Tiptap)
   - Pre-built templates
   - Mobile preview

2. **A/B Testing Framework**
   - Variant creation
   - Winner selection
   - Performance tracking

3. **Campaign Composer Improvements**
   - Merge field picker
   - Dynamic content blocks
   - Segment targeting

**Effort:** 2-3 weeks, 1 dev, Medium impact

---

### **Phase 5: UX Polish (Weeks 9-10)**
Priority: Improve discoverability and usability

1. **Collapsible Sidebar Navigation**
   - Quick access to modules
   - Pinned contacts
   - Global search

2. **Pipeline Kanban View**
   - Drag-drop deal/lead movement
   - Status-based columns
   - Quick actions

3. **Contact List Enhancements**
   - Better filtering
   - Bulk actions
   - Quick view panel

**Effort:** 2 weeks, 1-2 devs, Medium impact

---

## 9. SPECIFIC IMPLEMENTABLE FEATURES (Quick Wins)

### Feature: Email Performance Cards
**What:** Simple dashboard cards on `/campanhas/[id]/` showing: Sent, Opened, Clicked, Bounce
**Why:** Users need to see campaign results immediately
**Effort:** 1-2 days
**Impact:** High (addresses immediate pain point)
**Tech:** Aggregate `EmailQueueItem` counts, display with shadcn Cards

---

### Feature: Last Engagement Date
**What:** Show "Last engaged: 3 days ago" on contact record
**Why:** Quick visibility into contact activity
**Effort:** 1 day
**Impact:** Medium (quick win, used in segmentation)
**Tech:** Compute max(LastEmailOpenedAt, LastEmailClickedAt, LastNoteAddedAt, etc.)

---

### Feature: Tag-Based Filtering
**What:** Contacts table filter by tag (multi-select)
**Why:** Tags exist, but filtering is hard
**Effort:** 1-2 days
**Impact:** Medium (improves usability)
**Tech:** Add tag filter to table columns

---

### Feature: Contact Status Badge
**What:** Visual status indicator (Lead, Contacted, Qualified, etc.) with color coding
**Why:** Quick at-a-glance status recognition
**Effort:** 1 day
**Impact:** Low (cosmetic, but improves UX)
**Tech:** Tailwind badge component, color map per status

---

## 10. CONCLUSION & KEY TAKEAWAYS

**Where DIAX Excels:**
- Focused, single-user design (not over-engineered)
- Clean architecture (backend separation of concerns)
- Email integration via Brevo (industry standard)
- Flexible entity model (Customer ↔ Leads, Tags, etc.)

**Where DIAX Needs Improvement:**
- **Visibility:** No timeline, no engagement metrics visible
- **Guidance:** No lead scoring to tell sales which leads to focus on
- **Automation:** No workflows to act on behavioral triggers
- **Analytics:** No dashboards to understand campaign performance
- **UX:** Navigation, contact record layout, and search need work

**Top 3 Priorities (by ROI):**
1. **Contact Timeline & Activity Log** → Provides visibility into all interactions
2. **Lead Scoring** → Enables lead qualification and prioritization
3. **Email Campaign Analytics** → Shows campaign performance clearly

**Low-Hanging Fruit (Quick Wins):**
- Email performance summary cards
- Last engagement date tracking
- Contact detail page improvements
- Tag-based filtering

**Long-Term Vision:**
DIAX can evolve into a lightweight, single-user alternative to Pipedrive or HubSpot by adding intelligence (scoring, automation, analytics) and visual tools (timeline, funnel, kanban). The current foundation is solid; the gaps are in insight and usability.

---

## Sources

- [Pipedrive Email Marketing](https://www.pipedrive.com/en/products/email-marketing-software)
- [Pipedrive Marketing Automation](https://www.pipedrive.com/en/products/email-marketing-software/marketing-automation-tool)
- [Pipedrive Lead Qualification Guide](https://www.pipedrive.com/en/blog/lead-qualification)
- [HubSpot Email Campaign Analytics](https://knowledge.hubspot.com/marketing-email/analyze-your-marketing-email-campaign-performance)
- [HubSpot Engagement Scoring](https://knowledge.hubspot.com/scoring/build-engagement-scores)
- [HubSpot Custom Events & Timeline](https://developers.hubspot.com/docs/api-reference/crm-timeline-v3/guide)
- [Salesforce Einstein Lead Scoring](https://trailhead.salesforce.com/content/learn/modules/lead-scoring-and-grading-in-account-engagement/einstein-scoring-in-account-engagement)
- [Salesforce Email Integration](https://outfunnel.com/salesforce-email-marketing-integration/)
- [ActiveCampaign Segmentation](https://www.activecampaign.com/platform/segmentation)
- [ActiveCampaign Automation Triggers](https://help.activecampaign.com/hc/en-us/articles/218788707-Automation-triggers-explained)
- [Multi-Touch Attribution Guide](https://www.salesforce.com/marketing/multi-touch-attribution/)
- [CRM Data Management Best Practices 2026](https://monday.com/blog/crm-and-sales/crm-data-management/)
- [Lead Analytics Dashboard Metrics](https://monday.com/blog/crm-and-sales/lead-analytics-dashboard/)
