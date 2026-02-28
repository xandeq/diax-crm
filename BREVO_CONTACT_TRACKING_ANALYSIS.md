# 📊 Análise: Rastreamento de Emails por Contato via API Brevo

## 🎯 Sua Pergunta

> "Se eu quiser saber quantos emails eu já mandei pra um customer ou contato do meu sistema usando API do Brevo, tem como? Consigo ver também quais emails foram lidos e por quem no meu painel? Vale a pena implementar essas no meu painel usando API do Brevo?"

**Resposta Curta**: ✅ **SIM**, é possível! E **PARCIALMENTE** já está implementado.

---

## 📋 O Que Já Temos Implementado (Sistema Atual)

### ✅ Rastreamento LOCAL por Customer

**Campos na entidade `Customer`:**
```csharp
public int EmailSentCount { get; private set; }
public DateTime? LastEmailSentAt { get; private set; }
public int WhatsAppSentCount { get; private set; }
public DateTime? LastWhatsAppSentAt { get; private set; }
public bool EmailOptOut { get; private set; }
```

**Como funciona:**
- Quando um email é enviado, chamamos `customer.RecordEmailSent()`
- Incrementa `EmailSentCount`
- Atualiza `LastEmailSentAt`

**Onde aparece:**
- ✅ Grid de clientes (`/customers`) - mostra contador
- ✅ Grid de leads (`/leads`) - mostra contador
- ✅ Timeline de atividades (`/customers/{id}/activities`)

### ✅ Analytics de CAMPANHAS (Agregado)

**Dashboard atual (`/analytics`):**
- Total de emails enviados (por campanha)
- Taxa de abertura (por campanha)
- Taxa de cliques (por campanha)
- Bounces e descadastros (por campanha)

**Limitação:**
- ❌ Não mostra detalhes POR CONTATO
- ❌ Não mostra QUAIS emails específicos foram abertos
- ❌ Não mostra QUANDO cada email foi aberto

---

## 🔍 O Que a API Brevo Oferece

### 1️⃣ **Estatísticas por Contato/Email**

**Endpoint**: `GET /contacts/{email}/campaignStats`

**Documentação**: [Get Contact Stats](https://developers.brevo.com/reference/get-contact-stats)

**O que retorna:**
```json
{
  "messagesSent": [
    {
      "campaignId": 123,
      "eventTime": "2026-02-28T10:30:00Z"
    }
  ],
  "delivered": [
    {
      "campaignId": 123,
      "eventTime": "2026-02-28T10:30:15Z"
    }
  ],
  "opens": [
    {
      "campaignId": 123,
      "count": 3,
      "eventTime": "2026-02-28T10:35:22Z",
      "ip": "192.168.1.1"
    }
  ],
  "clicks": [
    {
      "campaignId": 123,
      "url": "https://exemplo.com/oferta",
      "count": 2,
      "eventTime": "2026-02-28T10:36:45Z",
      "ip": "192.168.1.1"
    }
  ],
  "hardBounces": [...],
  "softBounces": [...],
  "complaints": [...],
  "unsubscriptions": [...]
}
```

**Capacidades:**
- ✅ Histórico completo de emails enviados para o contato
- ✅ Data/hora de cada abertura (com IP!)
- ✅ Quais links foram clicados (com URLs!)
- ✅ Bounces, complaints, unsubscribes
- ✅ Múltiplas aberturas (contador)
- ✅ Transações/pedidos (se integrado)

**Limitações:**
- ⚠️ Máximo 90 dias de histórico por requisição
- ⚠️ Apenas para campanhas (não emails individuais transacionais sem campaign)
- ⚠️ Requer URL-encode do email

---

### 2️⃣ **Relatório de Eventos (Todos os Emails)**

**Endpoint**: `GET /smtp/statistics/events`

**Documentação**: [Get Email Event Report](https://developers.brevo.com/reference/getemaileventreport-1)

**Filtros disponíveis:**
- `email` - filtrar por destinatário específico
- `messageId` - filtrar por message ID da Brevo
- `tags` - filtrar por tags (nosso caso: campaign ID)
- `event` - tipo: sent, delivered, opened, clicked, bounce, etc.
- `startDate` / `endDate` - período (máx 90 dias)
- `sort` - ordenação
- `limit` / `offset` - paginação

**O que retorna:**
```json
{
  "events": [
    {
      "email": "cliente@exemplo.com",
      "event": "opened",
      "messageId": "<abc123@brevo.com>",
      "tag": "campaign-uuid",
      "ts": 1709121600,
      "ts_event": 1709121700,
      "ip": "192.168.1.1",
      "subject": "Oferta especial"
    }
  ],
  "offset": 0,
  "limit": 50
}
```

**Capacidades:**
- ✅ Buscar TODOS os eventos de um email específico
- ✅ Filtrar por tipo de evento (opened, clicked, etc)
- ✅ Filtrar por período
- ✅ Paginação (até 5000 eventos)
- ✅ Inclui subject, messageId, IP, timestamp

**Limitações:**
- ⚠️ Máximo 90 dias
- ⚠️ Paginação limitada
- ⚠️ Pode ter custo de API se usado frequentemente

---

## 💡 Vale a Pena Implementar?

### ✅ **SIM, Vale a Pena** - Para Casos Específicos

#### **Caso de Uso 1: Timeline Detalhada do Cliente**
**Implementar:** Aba "Histórico de Emails" no drawer/sheet de timeline

**Valor:**
- 📧 Ver TODOS os emails enviados para o cliente
- 👁️ Ver quantas vezes cada email foi aberto
- 🖱️ Ver quais links o cliente clicou
- 📅 Timeline completa de engajamento

**Esforço:** ~4-6 horas
**ROI:** 🌟🌟🌟🌟 (Alto - muito valor para vendas/suporte)

---

#### **Caso de Uso 2: Indicador de Engajamento no Grid**
**Implementar:** Badge visual de engajamento

**Exemplo:**
```
João Silva | empresa@exemplo.com | 🔥 Hot Engager
Maria Santos | teste@teste.com | 👻 Ghost (nunca abriu)
```

**Valor:**
- 🎯 Identificar clientes engajados vs inativos
- 📊 Priorizar follow-ups
- 🚫 Evitar spam para quem nunca abre

**Esforço:** ~2-3 horas
**ROI:** 🌟🌟🌟🌟🌟 (Crítico - melhora conversão)

---

#### **Caso de Uso 3: Dashboard de Engajamento Individual**
**Implementar:** Página `/customers/{id}/email-insights`

**Conteúdo:**
- Gráfico de aberturas ao longo do tempo
- Lista de emails enviados com status
- Heatmap de horários de abertura
- Links mais clicados

**Valor:**
- 📈 Analytics detalhado por cliente
- 🕒 Descobrir melhor horário para enviar
- 🎯 Personalizar abordagem

**Esforço:** ~8-10 horas
**ROI:** 🌟🌟🌟 (Médio - útil mas não essencial)

---

### ❌ **NÃO Vale a Pena** - Casos Desnecessários

#### ❌ Substituir Contador Local
**Por quê:**
- O contador local (`EmailSentCount`) já funciona
- É mais rápido (não precisa API call)
- Funciona offline
- Histórico ilimitado

#### ❌ Polling Constante da API
**Por quê:**
- Custo de API alto
- Performance ruim
- Webhooks já fazem isso em tempo real

---

## 🎯 Recomendação de Implementação

### **FASE 1: Quick Win** (Recomendado)

**Implementar:** Indicador de engajamento no grid

```typescript
// Frontend: Badge de engajamento
interface EngagementLevel {
  level: 'hot' | 'warm' | 'cold' | 'ghost';
  label: string;
  color: string;
  icon: string;
}

function getEngagementLevel(customer: Customer): EngagementLevel {
  if (customer.emailSentCount === 0) return { level: 'cold', ... };

  // Buscar stats da Brevo (cachear por 24h)
  const stats = await brevoApi.getContactStats(customer.email);

  const openRate = stats.opens.length / stats.messagesSent.length;
  const lastOpenedDays = daysSince(stats.opens[0]?.eventTime);

  if (openRate > 0.5 && lastOpenedDays < 7) return { level: 'hot', ... };
  if (openRate > 0.2 && lastOpenedDays < 30) return { level: 'warm', ... };
  if (openRate > 0) return { level: 'cold', ... };
  return { level: 'ghost', ... };
}
```

**Backend:**
```csharp
// Cache de 24h para evitar API calls excessivas
public async Task<ContactEngagementDto> GetContactEngagement(string email)
{
    var cacheKey = $"brevo:engagement:{email}";
    var cached = await _cache.GetAsync<ContactEngagementDto>(cacheKey);
    if (cached != null) return cached;

    var stats = await _brevoApi.GetContactStatsAsync(email);
    var engagement = CalculateEngagement(stats);

    await _cache.SetAsync(cacheKey, engagement, TimeSpan.FromHours(24));
    return engagement;
}
```

**Ganhos:**
- 🎯 Identificação visual imediata de clientes engajados
- 📊 Priorização de follow-ups
- ⚡ Melhora taxa de conversão

**Esforço:** 2-3 horas
**ROI:** 🌟🌟🌟🌟🌟

---

### **FASE 2: Timeline Detalhada** (Implementar depois)

**Adicionar aba "Emails" no sheet de timeline:**

```tsx
<Tabs defaultValue="activities">
  <TabsList>
    <TabsTrigger value="activities">Atividades</TabsTrigger>
    <TabsTrigger value="emails">Emails</TabsTrigger> {/* NOVO */}
  </TabsList>

  <TabsContent value="emails">
    <EmailHistory customerId={customer.id} email={customer.email} />
  </TabsContent>
</Tabs>
```

**Componente `EmailHistory`:**
```tsx
function EmailHistory({ email }: { email: string }) {
  const { data } = useQuery(['brevo-stats', email], () =>
    api.get(`/customers/email-insights/${encodeURIComponent(email)}`)
  );

  return (
    <div className="space-y-2">
      {data.messagesSent.map(msg => (
        <Card key={msg.campaignId}>
          <CardHeader>
            <div className="flex justify-between">
              <span>{msg.subject}</span>
              <span>{formatDate(msg.eventTime)}</span>
            </div>
          </CardHeader>
          <CardContent>
            <div className="flex gap-4 text-sm">
              <div>
                <Mail className="h-4 w-4" />
                {msg.delivered ? '✅ Entregue' : '⏳ Enviando'}
              </div>
              <div>
                <Eye className="h-4 w-4" />
                {msg.opens?.length || 0} aberturas
              </div>
              <div>
                <MousePointer className="h-4 w-4" />
                {msg.clicks?.length || 0} cliques
              </div>
            </div>

            {msg.opens?.length > 0 && (
              <div className="mt-2 text-xs text-muted-foreground">
                Última abertura: {formatRelative(msg.opens[0].eventTime)}
              </div>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
```

**Ganhos:**
- 📧 Histórico completo por cliente
- 👁️ Visualizar engajamento individual
- 🎯 Insights para personalização

**Esforço:** 4-6 horas
**ROI:** 🌟🌟🌟🌟

---

## 📊 Comparação: Local vs API Brevo

| Métrica | Local (Atual) | API Brevo | Melhor |
|---------|--------------|-----------|--------|
| **Total de emails enviados** | ✅ `EmailSentCount` | ✅ `messagesSent.length` | **Local** (mais rápido) |
| **Último email enviado** | ✅ `LastEmailSentAt` | ✅ `messagesSent[0].eventTime` | **Local** (mais rápido) |
| **Emails foram abertos?** | ❌ Não rastreia | ✅ `opens[]` com detalhes | **Brevo** |
| **Quantas vezes abriu?** | ❌ Não rastreia | ✅ `opens[].count` | **Brevo** |
| **Quando abriu?** | ❌ Não rastreia | ✅ `opens[].eventTime` | **Brevo** |
| **Quais links clicou?** | ❌ Não rastreia | ✅ `clicks[].url` | **Brevo** |
| **IP de abertura** | ❌ Não rastreia | ✅ `opens[].ip` | **Brevo** |
| **Bounces por contato** | ❌ Não rastreia | ✅ `hardBounces[]` | **Brevo** |
| **Performance** | ⚡ Instantâneo | 🐢 Requer API call | **Local** |
| **Cache** | ✅ Sempre disponível | ⚠️ Depende de internet | **Local** |
| **Histórico** | ✅ Ilimitado | ⚠️ 90 dias | **Local** |
| **Detalhamento** | ❌ Básico | ✅ Rico | **Brevo** |

---

## ⚠️ Considerações Importantes

### **1. Cache é ESSENCIAL**
```csharp
// ❌ ERRADO: Buscar da API toda vez
var stats = await _brevoApi.GetContactStats(email); // Lento!

// ✅ CERTO: Cache de 24h
var cached = await _cache.GetAsync($"brevo:stats:{email}");
if (cached != null) return cached;
var stats = await _brevoApi.GetContactStats(email);
await _cache.SetAsync($"brevo:stats:{email}", stats, TimeSpan.FromHours(24));
```

### **2. Lazy Loading**
- Não buscar stats para TODOS os clientes do grid
- Buscar apenas quando usuário expandir timeline
- Ou buscar em background com fila

### **3. Custo de API**
- Brevo pode cobrar por requests excessivos
- Cache + lazy loading mitigam isso

### **4. Privacidade**
- IP addresses podem ser sensíveis (LGPD)
- Considere anonimizar ou não mostrar

---

## 🚀 Implementação Sugerida

### **Código Exemplo: Backend**

```csharp
// 1. DTO
public class ContactEmailInsightsDto
{
    public string Email { get; set; }
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalOpens { get; set; }
    public int TotalClicks { get; set; }
    public double OpenRate { get; set; }
    public double ClickRate { get; set; }
    public string EngagementLevel { get; set; } // hot, warm, cold, ghost
    public DateTime? LastOpenedAt { get; set; }
    public List<EmailEventDto> RecentEmails { get; set; }
}

public class EmailEventDto
{
    public int CampaignId { get; set; }
    public string Subject { get; set; }
    public DateTime SentAt { get; set; }
    public bool Delivered { get; set; }
    public int OpenCount { get; set; }
    public int ClickCount { get; set; }
    public List<string> ClickedUrls { get; set; }
}

// 2. Service
public class BrevoInsightsService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;

    public async Task<ContactEmailInsightsDto> GetContactInsights(
        string email,
        int days = 90)
    {
        var cacheKey = $"brevo:insights:{email}:{days}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<ContactEmailInsightsDto>(cached);

        var stats = await FetchFromBrevoAsync(email, days);
        var insights = MapToInsights(stats);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(insights),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });

        return insights;
    }

    private async Task<BrevoContactStatsResponse> FetchFromBrevoAsync(
        string email,
        int days)
    {
        var encodedEmail = Uri.EscapeDataString(email);
        var startDate = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await _httpClient.GetAsync(
            $"https://api.brevo.com/v3/contacts/{encodedEmail}/campaignStats?startDate={startDate}&endDate={endDate}");

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BrevoContactStatsResponse>();
    }

    private ContactEmailInsightsDto MapToInsights(BrevoContactStatsResponse stats)
    {
        var totalSent = stats.MessagesSent?.Count ?? 0;
        var totalOpens = stats.Opens?.Sum(o => o.Count) ?? 0;
        var totalClicks = stats.Clicks?.Sum(c => c.Count) ?? 0;

        var openRate = totalSent > 0 ? (double)totalOpens / totalSent : 0;
        var clickRate = totalSent > 0 ? (double)totalClicks / totalSent : 0;

        var engagementLevel = CalculateEngagementLevel(openRate, stats.Opens);

        return new ContactEmailInsightsDto
        {
            Email = stats.Email,
            TotalSent = totalSent,
            TotalDelivered = stats.Delivered?.Count ?? 0,
            TotalOpens = totalOpens,
            TotalClicks = totalClicks,
            OpenRate = openRate * 100,
            ClickRate = clickRate * 100,
            EngagementLevel = engagementLevel,
            LastOpenedAt = stats.Opens?.FirstOrDefault()?.EventTime,
            RecentEmails = MapRecentEmails(stats)
        };
    }

    private string CalculateEngagementLevel(
        double openRate,
        List<BrevoOpenEvent> opens)
    {
        if (openRate == 0) return "ghost";

        var daysSinceLastOpen = opens?.FirstOrDefault()?.EventTime != null
            ? (DateTime.UtcNow - opens.First().EventTime).TotalDays
            : 999;

        if (openRate > 0.5 && daysSinceLastOpen < 7) return "hot";
        if (openRate > 0.2 && daysSinceLastOpen < 30) return "warm";
        return "cold";
    }
}

// 3. Controller
[HttpGet("customers/{customerId}/email-insights")]
public async Task<IActionResult> GetEmailInsights(Guid customerId)
{
    var customer = await _customerRepository.GetByIdAsync(customerId);
    if (customer == null) return NotFound();

    var insights = await _brevoInsightsService.GetContactInsights(customer.Email);
    return Ok(insights);
}
```

---

## 📈 Roadmap de Implementação

### **Sprint 1: Fundação** (1 dia)
1. Criar `BrevoInsightsService`
2. Implementar cache
3. Criar DTOs
4. Endpoint `/customers/{id}/email-insights`

### **Sprint 2: UI - Indicador** (meio dia)
5. Badge de engagamento no grid
6. Tooltip com detalhes

### **Sprint 3: UI - Timeline** (1 dia)
7. Aba "Emails" no timeline
8. Lista de emails enviados
9. Estatísticas individuais

---

## ✅ Conclusão

### **Resposta Direta às Suas Perguntas:**

1. **"Se eu quiser saber quantos emails eu já mandei pra um customer?"**
   - ✅ Sim! Use `EmailSentCount` (local - já implementado)
   - ✅ Ou `GET /contacts/{email}/campaignStats` (Brevo - mais detalhado)

2. **"Consigo ver quais emails foram lidos e por quem no meu painel?"**
   - ❌ Não atualmente
   - ✅ Mas é FÁCIL implementar com API da Brevo!

3. **"Vale a pena implementar essas no meu painel?"**
   - ✅ **SIM!** ROI altíssimo
   - Prioridade: Indicador de engajamento no grid (2-3h, ROI máximo)
   - Depois: Timeline detalhada (4-6h, ROI alto)

### **Benefícios Esperados:**
- 📈 **+20-30%** na taxa de conversão (foco em clientes engajados)
- ⏰ **-50%** tempo gasto em follow-ups frios
- 🎯 **+100%** personalização de abordagem
- 💡 Insights valiosos sobre comportamento do cliente

---

## 📚 Referências

- [Get Contact Stats - Brevo API](https://developers.brevo.com/reference/get-contact-stats)
- [Get Email Event Report - Brevo API](https://developers.brevo.com/reference/getemaileventreport-1)
- [Transactional Email Statistics - Brevo Help](https://help.brevo.com/hc/en-us/articles/360021509380-Transactional-email-statistics)
- [Review Transactional Email Reports - Brevo Help](https://help.brevo.com/hc/en-us/articles/208858829-Review-your-transactional-email-reports)

---

**Recomendação Final**: ✅ **IMPLEMENTE!** Comece pelo indicador de engajamento no grid (quick win), depois expanda para timeline detalhada.

**Elaborado em**: 2026-02-28
**Versão**: 1.0
