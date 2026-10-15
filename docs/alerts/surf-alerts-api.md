# Surf Alerts API

Endpoint de ingestão para automações externas (ex: previsão semanal de surf cruzada
com agenda) postarem um alerta no CRM.

## Gerar a API Key (uma vez)

Logado no CRM (JWT), via Swagger ou curl:

```bash
curl -X POST https://api.alexandrequeiroz.com.br/api/v1/apikeys \
  -H "Authorization: Bearer <seu JWT>" \
  -H "Content-Type: application/json" \
  -d '{"name":"perplexity-computer-surf","validityDays":3650,"scope":"surf-alerts.ingest"}'
```

A resposta traz `apiKey` (prefixo `dk_live_...`) **uma única vez** — copie e guarde em
`~/.claude/.secrets.env` (ou no cofre do emissor). Para também listar os alertas pelo
painel (não necessário para a automação em si), use o escopo combinado
`"surf-alerts.ingest,surf-alerts.view"` — a leitura via API, no entanto, roda sob JWT
(ver `GET` abaixo), então esse segundo escopo normalmente não é usado.

## POST /api/v1/surf-alerts

```
POST https://api.alexandrequeiroz.com.br/api/v1/surf-alerts
Content-Type: application/json
X-Alert-Api-Key: dk_live_...
Idempotency-Key: surf-2026-10-17   (opcional — mesma chave não duplica)
```

```json
{
  "source": "perplexity-computer",
  "type": "surf_forecast_weekend",
  "generated_at": "2026-10-16T07:02:00-03:00",
  "weekend": { "saturday": "2026-10-17", "sunday": "2026-10-18" },
  "has_good_window": true,
  "summary": "Melhor janela: sábado 06:00-09:00 na Barra do Jucu, ondas 1,2-1,5m, vento terral NO 8km/h.",
  "best_window": {
    "date": "2026-10-17", "start": "06:00", "end": "09:00", "spot": "Barra do Jucu",
    "wave_height_m": { "min": 1.2, "max": 1.5 },
    "swell": { "direction": "SE", "period_s": 10 },
    "wind": { "speed_kmh": 8, "gust_kmh": 12, "direction": "NO", "type": "terral" },
    "tide": "enchendo, maré baixa às 05:40",
    "weather": "Parcialmente nublado, 24°C, 10% de chuva",
    "rating": 4,
    "calendar_free": true
  },
  "alternatives": [],
  "calendar_conflicts": [],
  "sources": ["https://www.surfguru.com.br/...", "https://www.windguru.cz/..."]
}
```

Campos obrigatórios: `source`, `type`, `generated_at`, `weekend.saturday`,
`weekend.sunday`, `summary` (máx. 1000 chars). `best_window` pode ser omitido/`null`
quando não houver janela boa.

### Respostas

| Status | Quando |
| --- | --- |
| `201 Created` | Alerta criado. Corpo: `{ "id": "...", "status": "created" }` |
| `200 OK` | Mesmo `Idempotency-Key` já recebido — devolve o registro existente, sem duplicar |
| `400 Bad Request` | Campo obrigatório ausente ou `weekend.sunday` anterior a `weekend.saturday` |
| `401 Unauthorized` | Header `X-Alert-Api-Key` ausente, inválido, desabilitado ou expirado |
| `403 Forbidden` | Chave sem o escopo `surf-alerts.ingest` |
| `429 Too Many Requests` | Mais de 30 requisições/hora com a mesma chave |

Quando `has_good_window` é `true`, o CRM dispara automaticamente um alerta no Telegram
(reaproveita o canal operacional já existente — `IEmailOpsAlerter`), com cooldown por
fim de semana (`surf-alert:<sábado>`), então reenvios do mesmo fim de semana não
inundam o canal.

## GET /api/v1/surf-alerts?limit=10

Lista os alertas mais recentes. Autenticação por sessão logada (JWT), não pela API Key
— é a mesma convenção do resto do CRM (toda leitura autenticada do painel usa JWT).

```bash
curl https://api.alexandrequeiroz.com.br/api/v1/surf-alerts?limit=10 \
  -H "Authorization: Bearer <seu JWT>"
```

## Exemplo de automação (o caso de uso original)

Toda sexta-feira, uma automação (ex: Perplexity Computer, n8n, cron) consulta a
previsão de ondas/vento/clima para Vila Velha e região, cruza com a agenda do Google
Calendar do fim de semana, e faz o `POST` acima com a melhor janela encontrada.
