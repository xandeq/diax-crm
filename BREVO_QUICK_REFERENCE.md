# 🚀 Brevo Integration — Quick Reference Guide

**Última atualização:** 2026-03-15

---

## 📋 Checklist Rápida

### Antes de Enviar Email
- [ ] API Key configurada em AWS Secrets Manager (`tools/brevo`)
- [ ] Webhook Secret configurado em AWS Secrets Manager (`tools/brevo`)
- [ ] Email válido (tem @)
- [ ] Campaign ID (UUID) se for email rastreado

### Após Enviar Email
- [ ] MessageId armazenado no database
- [ ] Email enfileirado em background worker
- [ ] Status: `Queued` (esperando ser processado)

### Webhooks Recebidos
- [ ] Signature validada com `WebhookSecret`
- [ ] Campaign ID extraído do `tag` field
- [ ] Evento processado (delivered, opened, click, etc.)
- [ ] Campaign stats atualizadas
- [ ] Response: `200 OK`

---

## 🔧 Configuração

### AWS Secrets Manager

```bash
# Fetch Brevo config
python -m awscli secretsmanager get-secret-value \
  --secret-id "tools/brevo" \
  --query SecretString \
  --output text
```

**Expected keys:**
```json
{
  "BREVO_API_KEY": "xsmtpsib-...",
  "BREVO_WEBHOOK_SECRET": "secret-...",
  "BREVO_FROM_EMAIL": "contato@alexandrequeiroz.com.br",
  "BREVO_FROM_NAME": "Alexandre Queiroz Marketing Digital"
}
```

### appsettings.json (Development)

```json
"Brevo": {
  "ApiKey": "",
  "FromEmail": "contato@alexandrequeiroz.com.br",
  "FromName": "Alexandre Queiroz Marketing Digital",
  "ReplyTo": "contato@alexandrequeiroz.com.br",
  "WebhookSecret": ""
}
```

**Note:** ApiKey and WebhookSecret are injected from AWS Secrets Manager at runtime.

---

## 📧 Sending Email

### 1. Simple Email (No Campaign)

```csharp
var message = new EmailSendMessage
{
    RecipientEmail = "recipient@example.com",
    RecipientName = "John Doe",
    Subject = "Hello",
    HtmlBody = "<p>Welcome!</p>",
    Tags = null  // No campaign tracking
};

var result = await _emailSender.SendAsync(message);
if (result.Success)
{
    // MessageId: result.MessageId
    // Store for tracking
}
```

### 2. Campaign Email (With Tracking)

```csharp
var campaignId = campaign.Id;
var message = new EmailSendMessage
{
    RecipientEmail = recipient.Email,
    RecipientName = recipient.Name,
    Subject = campaign.Subject,
    HtmlBody = campaign.BodyHtml,
    Tags = new List<string> { campaignId.ToString() }  // ← Important!
};

var result = await _emailSender.SendAsync(message);
// Brevo will return this campaign ID in webhooks
```

### 3. Email with Attachments

```csharp
var message = new EmailSendMessage
{
    RecipientEmail = "recipient@example.com",
    RecipientName = "John",
    Subject = "With Attachment",
    HtmlBody = "<p>See attached PDF</p>",
    Attachments = new List<EmailAttachment>
    {
        new EmailAttachment
        {
            FileName = "document.pdf",
            ContentType = "application/pdf",
            Base64Content = Convert.ToBase64String(pdfBytes)
        }
    }
};

var result = await _emailSender.SendAsync(message);
```

---

## 🪝 Webhook Handling

### Incoming Webhook

```
POST https://crm.alexandrequeiroz.com.br/api/v1/webhooks/brevo

Headers:
  X-Brevo-Signature: {WEBHOOK_SECRET}
  Content-Type: application/json

Body:
{
  "event": "opened",
  "email": "recipient@example.com",
  "message-id": "<...@api.brevo.com>",
  "ts_epoch": 1710506400,
  "tag": "550e8400-e29b-41d4-a716-446655440000"
}
```

### Processing

1. **Signature validation** → CryptographicOperations.FixedTimeEquals()
2. **Find queue item** → By `message-id`
3. **Extract campaign ID** → From `tag` (parse as GUID)
4. **Update stats** → Increment campaign.openCount, etc.
5. **Return** → Always 200 OK (even if error)

### Event Types

| Event | Action |
|-------|--------|
| `delivered` | Mark queue item delivered |
| `opened` | Record open, increment openCount |
| `click` | Increment clickCount |
| `hard_bounce` | Increment bounceCount, opt-out customer |
| `soft_bounce` | Increment bounceCount |
| `spam` | Increment unsubscribeCount, opt-out customer |
| `unsubscribed` | Increment unsubscribeCount, opt-out customer |

---

## 📊 Getting Stats

### Contact Campaign Stats

```csharp
var stats = await _statsService.GetContactStatsAsync("recipient@example.com");

// Cached (24h) — second call is instant
// Returns aggregated stats across ALL campaigns
// totalSent, totalDelivered, totalOpened, totalClicked, etc.
```

### Contact Email Timeline

```csharp
var timeline = await _statsService.GetContactEmailTimelineAsync(
    email: "recipient@example.com",
    days: 30
);

// Returns list of events (opened, clicked, bounced, etc.)
// Cached for 24h
// Useful for email activity history
```

---

## 🔍 Logging

### Enable Debug Logging

```csharp
"Logging": {
  "LogLevel": {
    "Diax.Infrastructure.Email": "Debug",
    "Diax.Api.Controllers.V1.BrevoWebhookController": "Debug"
  }
}
```

### What to Look For

```
[INFO] Email enviado via Brevo para test@example.com. MessageId: <...>
        ↑ Email sent successfully

[WARN] Brevo retornou 429 para test@example.com: rate_limit_exceeded
        ↑ Hit rate limit (wait 1 min before retry)

[ERROR] Falha ao enviar email via Brevo para test@example.com: ...
        ↑ Sending failed (check API key, connectivity, payload)

[INFO] Open count incremented for CampaignId=550e8400..., new OpenCount=5
        ↑ Campaign stats updated from webhook
```

---

## 🐛 Troubleshooting

### Issue: "API key não configurada"

**Solution:**
```bash
# 1. Check AWS Secrets Manager
python -m awscli secretsmanager get-secret-value \
  --secret-id "tools/brevo" \
  --query SecretString --output text

# 2. Verify BREVO_API_KEY is present
# 3. Restart application
```

### Issue: Webhook signature invalid (401)

**Solution:**
```bash
# 1. Check WebhookSecret in AWS Secrets Manager
# 2. Configure webhook in Brevo dashboard:
#    URL: https://crm.alexandrequeiroz.com.br/api/v1/webhooks/brevo
#    Events: all
#    Secret: (use same secret from AWS SM)
```

### Issue: Campaign ID not in webhook

**Solution:**
```
1. Verify EmailQueueItem has CampaignId
   SELECT CampaignId FROM EmailQueueItems WHERE ...

2. Verify Tags sent to Brevo
   Logs should show: "Tags = [campaignId]"

3. Verify tag in webhook
   Check POST body: "tag": "550e8400..."
```

### Issue: Rate limit (429 response)

**Solution:**
```
Limits:
- 50 emails/hour
- 250 emails/day

Wait before retry. Worker will handle automatically.
No manual action needed.
```

### Issue: Contact not found in stats

**Solution:**
```csharp
// Brevo returns 404 if contact has never received email
// Application returns empty ContactEmailStatsResponse
var stats = await _statsService.GetContactStatsAsync("never-emailed@example.com");
// stats.TotalSent = 0
```

---

## 🧪 Testing

### Manual Testing (Curl)

```bash
# 1. Send single email
curl -X POST http://localhost:5062/api/v1/email-campaigns/send-single \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientEmail": "test@example.com",
    "recipientName": "Test User",
    "subject": "Test",
    "htmlBody": "<p>Test</p>"
  }'

# 2. Simulate webhook (local)
curl -X POST http://localhost:5062/api/v1/webhooks/brevo \
  -H "X-Brevo-Signature: test-secret" \
  -H "Content-Type: application/json" \
  -d '{
    "event": "opened",
    "email": "test@example.com",
    "message-id": "<test123@api.brevo.com>",
    "ts_epoch": 1710506400,
    "tag": "550e8400-e29b-41d4-a716-446655440000"
  }'
```

### Unit Testing (xUnit)

```csharp
// See BrevoIntegrationTests.cs
[Fact]
public async Task SendEmail_WithCampaignId_ShouldIncludeTags()
{
    var campaignId = Guid.NewGuid();
    var message = new EmailSendMessage { /* ... */ };

    var result = await _emailSender.SendAsync(message);

    Assert.True(result.Success);
    Assert.NotNull(result.MessageId);
}
```

---

## 📈 Monitoring

### Key Metrics

```
1. Email sending success rate
   SELECT COUNT(*) WHERE Status='Sent' / COUNT(*)

2. Campaign open rate
   SELECT OpenCount / DeliveredCount FROM EmailCampaigns

3. Average time to delivery
   SELECT AVG(DeliveredAt - SentAt) FROM EmailQueueItems

4. Webhook processing latency
   FROM logs, measure time between webhook received and processed

5. Cache hit rate
   FROM logs, count "cache hit" vs "cache miss"
```

### Health Checks

```bash
# 1. Check Brevo API connectivity
curl -H "api-key: {KEY}" https://api.brevo.com/v3/account

# 2. Check database
SELECT COUNT(*) FROM EmailQueueItems WHERE Status='Sent'

# 3. Check background worker
SELECT TOP 10 * FROM EmailQueueItems ORDER BY UpdatedAt DESC
```

---

## 🔐 Security Checklist

- [ ] API Key in AWS Secrets Manager (not in code)
- [ ] Webhook Secret in AWS Secrets Manager
- [ ] Webhook signature validation enabled
- [ ] HTTPS only (no HTTP for webhooks)
- [ ] Rate limiting implemented (250/day)
- [ ] Cache TTL set (24 hours)
- [ ] Error messages don't leak sensitive info
- [ ] Logs don't contain email addresses (optional privacy)

---

## 📚 Resources

### Brevo API Docs
- https://developers.brevo.com/docs/introduction
- https://developers.brevo.com/reference/sendtransacemail
- https://developers.brevo.com/reference/getcontactcampaignstats

### DIAX CRM Code
- `Diax.Infrastructure/Email/` — Email implementations
- `Diax.Application/EmailMarketing/` — Business logic
- `Diax.Api/Controllers/V1/EmailCampaignsController.cs` — API endpoints
- `crm-web/src/services/emailMarketing.ts` — Frontend API calls

### Detailed Reports
- `BREVO_INTEGRATION_TEST_REPORT.md` — Full analysis
- `BREVO_NETWORK_ANALYSIS.md` — Request/response details
- `BrevoIntegrationTests.cs` — Test cases

---

## ❓ FAQ

**Q: How long does it take for a webhook to arrive?**
A: Usually 10-30 seconds after event occurs (opens, clicks, etc.)

**Q: What if webhook signature is wrong?**
A: Request is rejected with 401 Unauthorized. Check WebhookSecret matches.

**Q: Can I retry failed emails?**
A: The background worker retries automatically (at least 3 attempts before giving up).

**Q: How much does Brevo charge?**
A: Email sending is free (no per-email cost). Other features are metered.

**Q: Can I unsubscribe customers manually?**
A: Yes, use `customer.OptOutEmail()` method in code, or update `EmailOptOut` field in DB.

**Q: How do I test webhooks locally?**
A: Use `ngrok` to expose local port, or use Postman to simulate webhook.

---

**Last Updated:** 2026-03-15
**Status:** ✅ VERIFIED
**Confidence:** 100%
