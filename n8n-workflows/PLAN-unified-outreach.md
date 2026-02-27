# PLAN: Unified Lead Outreach Engine (Email + WhatsApp)

## STATUS: Ready for implementation

## DECISIONS (confirmed by user)
1. Make `email` nullable in Customer entity (migration)
2. Add `GoogleMaps = 11` to LeadSource enum
3. Evolution API at same VPS: `https://evolution-api.h8tqhp.easypanel.host`
4. Brand: "Alexandre Queiroz Marketing Digital" in all messages
5. SQL Server connection: `Server=tcp:sql1002.site4now.net;Database=db_aaf0a8_diaxcrm`

---

## DATA INSIGHT (Apify Google Maps - Vila Velha ES, 50 leads)

| Contact Type  | Count | Coverage |
|---------------|-------|----------|
| Has Phone     | 48    | **96%**  |
| Has Website   | 27    | 54%      |
| Has Email     | 9     | **18%**  |
| No Contact    | 2     | 4%       |

**Strategy: WhatsApp-first (96% reach) + Email for the 18% that have it.**

---

## ARCHITECTURE

```
CRM (SQL Server) = Source of Truth
n8n (VPS EasyPanel) = Automation Engine

                    +------------------+
                    |   DIAX CRM       |
                    |  (SQL Server)    |
                    |  customers table |
                    |  outreach_logs   |
                    |  email_campaigns |
                    |  email_queue     |
                    +--------+---------+
                             |
              +--------------+--------------+
              |                             |
      +-------v--------+          +--------v--------+
      |  CRM .NET API  |          |  n8n Workflows  |
      |  /api/v1/...   |   -->>   |  (VPS EasyPanel)|
      +----------------+          +--------+--------+
                                           |
                         +-----------------+-----------------+
                         |                 |                 |
                  +------v-----+   +------v------+   +------v------+
                  |   Apify    |   |    SMTP     |   | Evolution   |
                  |  (Import)  |   |  (Email)    |   |  API (WA)   |
                  +------------+   +-------------+   +-------------+
```

---

## IMPLEMENTATION STEPS

### Step 1: CRM C# Code Changes

**1a. Make Email nullable in Customer.cs**
- `Customer.cs`: Change `string Email` -> `string? Email`
- Constructor: Make `email` parameter nullable with default null
- `UpdateBasicInfo()`: Accept `string? email`
- `CustomerConfiguration.cs`: Remove any required constraint on Email

**1b. Add GoogleMaps = 11 to LeadSource.cs**
```csharp
GoogleMaps = 11
```

**1c. Create OutreachLog entity**
New file: `Diax.Domain/Outreach/OutreachLog.cs`
- Fields: CustomerId, Channel (enum: Email/WhatsApp), TemplateName, MessagePreview, Status (enum: Sent/Delivered/Read/Replied/Failed), SentAt, ErrorMessage, ProviderMessageId, CampaignId
- Inherits AuditableEntity + IUserOwnedEntity

**1d. Create EF migration**
- Make email nullable in customers table
- Add outreach_logs table
- Update LeadSource to include GoogleMaps

### Step 2: Build Unified n8n Workflow

Replace the current MySQL-based workflow with a SQL Server workflow that has **4 modules**:

**MODULE 1: IMPORT (Apify -> CRM customers)**
```
Webhook/Manual Trigger
  -> HTTP Request (Apify dataset)
  -> Code: Normalize Apify data to CRM schema
     - title -> name, company_name
     - emails[0] -> email (nullable)
     - phone -> phone (clean +55 format)
     - whatsapps[0] -> whats_app (extract from wa.me URL)
     - website -> website
     - categoryName -> tags
     - source = 11 (GoogleMaps)
     - source_details = "Apify Google Maps - {search query}"
     - status = 0 (Lead)
     - person_type = 1 (Company)
  -> Code: Validate (email format if present, phone format)
  -> Code: Deduplicate within batch
  -> SQL Server: Check existing by phone OR email
  -> IF new -> INSERT INTO customers
  -> IF exists -> UPDATE (merge new data)
```

**MODULE 2: SEGMENTATION (channel + business potential)**
```
Schedule/Webhook Trigger
  -> SQL Server: Fetch customers WHERE status = 0 (Lead) AND source = 11
  -> Code: Segment
     - Channel: has_phone -> WhatsApp, has_email -> Email, both -> Both
     - Priority: no website -> HOT (offer creation), has website -> WARM (offer SEO)
  -> SQL Server: Update customer tags with segment info
```

**MODULE 3: WHATSAPP OUTREACH (via Evolution API)**
```
Schedule/Webhook Trigger
  -> SQL Server: Fetch segmented leads with phone, not contacted recently
  -> SplitInBatches (5 per batch)
  -> Code: Prepare WhatsApp message (Portuguese, personalized)
  -> HTTP Request: POST evolution-api.h8tqhp.easypanel.host/message/sendText/{instance}
  -> SQL Server: INSERT INTO outreach_logs (channel='whatsapp')
  -> SQL Server: UPDATE customers SET last_contact_at, status = 1 (Contacted)
  -> Wait 30s (WhatsApp rate limit)
```

**MODULE 4: EMAIL OUTREACH (SMTP for leads with email)**
```
Schedule/Webhook Trigger
  -> SQL Server: Fetch segmented leads with email, not contacted recently
  -> SplitInBatches (10 per batch)
  -> Code: Prepare email (HTML, Portuguese, personalized)
  -> SMTP Send
  -> SQL Server: INSERT INTO outreach_logs (channel='email')
  -> SQL Server: UPDATE customers SET last_contact_at, status = 1 (Contacted)
  -> Wait 15s
```

### Step 3: Message Templates

**WhatsApp HOT (no website):**
```
Ola! Sou o Alexandre, da Alexandre Queiroz Marketing Digital.
Vi que a {company_name} em {city} ainda nao tem um site.
Hoje ter presenca online e essencial para ser encontrado no Google.
Posso criar um site profissional para voce. Quer saber mais?
```

**WhatsApp WARM (has website):**
```
Ola! Sou o Alexandre, da Alexandre Queiroz Marketing Digital.
Visitei o site da {company_name} e tenho algumas sugestoes
para melhorar o posicionamento no Google.
Posso compartilhar uma analise gratuita?
```

**Email HOT (no website):**
- Subject: "Site Profissional para {company_name} - Alexandre Queiroz Marketing Digital"
- HTML with portfolio, CTA to WhatsApp

**Email WARM (has website):**
- Subject: "Analise Gratuita do Site da {company_name}"
- HTML with SEO tips preview, CTA to WhatsApp

### Step 4: Push to n8n + Test

- Build final JSON workflow
- Push via n8n REST API (X-N8N-API-KEY)
- Create SQL Server credential in n8n
- Create SMTP credential in n8n
- Run EF migration on production SQL Server
- Test Module 1 (import) with Manual Trigger
- Test Module 3 (WhatsApp) with 1-2 leads

---

## CRM TABLE: customers (existing, with changes)

Column mapping for n8n SQL queries (snake_case per EF convention):
```sql
-- Existing columns used:
id                  -- UNIQUEIDENTIFIER PK
name                -- NVARCHAR(256) - company title from Apify
company_name        -- NVARCHAR(256) - same as name for companies
email               -- NVARCHAR(256) - NOW NULLABLE
phone               -- NVARCHAR(50)
whats_app           -- NVARCHAR(50) - extracted from wa.me URL
website             -- NVARCHAR(256)
source              -- INT (11 = GoogleMaps)
source_details      -- NVARCHAR(256) - "Apify Google Maps - Vila Velha"
tags                -- NVARCHAR(256) - "restaurante,vila-velha,google-maps"
status              -- INT (0=Lead, 1=Contacted, 2=Qualified...)
person_type         -- INT (1=Company)
notes               -- NVARCHAR(256) - review summary or address
last_contact_at     -- DATETIME2 NULL
created_at          -- DATETIME2
created_by          -- NVARCHAR(256) - "n8n-automation"
updated_at          -- DATETIME2 NULL
updated_by          -- NVARCHAR(256) NULL
```

## NEW TABLE: outreach_logs

```sql
id                  -- UNIQUEIDENTIFIER PK
user_id             -- UNIQUEIDENTIFIER FK -> users
customer_id         -- UNIQUEIDENTIFIER FK -> customers
channel             -- INT (0=Email, 1=WhatsApp)
template_name       -- NVARCHAR(200)
message_preview     -- NVARCHAR(500)
status              -- INT (0=Sent, 1=Delivered, 2=Read, 3=Replied, 4=Failed)
sent_at             -- DATETIME2
error_message       -- NVARCHAR(2000) NULL
provider_message_id -- NVARCHAR(255) NULL
campaign_id         -- UNIQUEIDENTIFIER NULL FK -> email_campaigns
created_at          -- DATETIME2
created_by          -- NVARCHAR(256)
updated_at          -- DATETIME2 NULL
updated_by          -- NVARCHAR(256) NULL
```

---

## RATE LIMITS & ANTI-SPAM

| Channel   | Per Batch | Delay   | Daily Max | Cooldown    |
|-----------|-----------|---------|-----------|-------------|
| WhatsApp  | 5         | 30s     | 50/day    | 7 days      |
| Email     | 10        | 15s     | 200/day   | 7 days      |

---

## FILE ORGANIZATION (final)

```
CRM/
  n8n-workflows/
    unified-outreach.json          (NEW - main workflow with all 4 modules)
    whatsapp-routing.json          (existing)
    n8n-workflow.json              (existing - tech news)
    PLAN-unified-outreach.md       (this file)
  api-core/
    src/Diax.Domain/
      Customers/Customer.cs                    (MODIFY - email nullable)
      Customers/Enums/LeadSource.cs            (MODIFY - add GoogleMaps)
      Outreach/OutreachLog.cs                  (NEW)
      Outreach/Enums/OutreachChannel.cs        (NEW)
      Outreach/Enums/OutreachStatus.cs         (NEW)
    src/Diax.Infrastructure/
      Data/DiaxDbContext.cs                     (MODIFY - add DbSet)
      Data/Configurations/
        OutreachLogConfiguration.cs            (NEW)
        CustomerConfiguration.cs               (MODIFY if needed)
      Data/Migrations/
        YYYYMMDD_AddOutreachAndGoogleMaps.cs   (NEW migration)
```
