# Plano — Campanhas de Email Segmentadas: Serviços Alexandre Queiroz

**Criado:** 2026-06-16
**Plataforma de envio:** DIAX CRM (.NET 8) — sistema de email maduro (6 provedores, webhook tracking, supressão/LGPD, IA de assunto)
**Fonte de leads:** Extrator-DIAX (campo `category` = nicho, `city`, ~600–900 leads/dia, 19 nichos)
**Início:** Piloto com 1 nicho + 1 serviço, depois escala controlada
**Design dos templates:** skill `frontend-design` → adaptado para HTML email-safe (tabelas + CSS inline)

---

## Dados da marca (fixos em todo email)

- **Empresa:** Alexandre Queiroz Marketing Digital
- **Site:** https://www.alexandrequeiroz.com.br
- **Remetente:** contato@alexandrequeiroz.com.br (já configurado no DIAX/Brevo como FROM)
- **WhatsApp:** (27) 99984-0101 (alt: 27 9968-8744)
- **Logo:** `D:\claude-code\website-aq\images\logo.png` (180×48) + `logo.webp`
- **Cor da marca:** `#57b3df` (azul ciano)
- **Região:** Vitória/ES (LocalBusiness)

## Os 4 serviços

1. **Sites** — criação/desenvolvimento de sites
2. **Apps** — aplicativos mobile (iOS/Android)
3. **Software sob demanda** — sistemas/softwares sob medida para empresas
4. **Landing pages** — páginas de conversão

## Nichos disponíveis (Extrator `niches`, 19)

restaurante · academia · clínica médica · dentista · advocacia · contabilidade · imobiliária · salão de beleza · farmácia · supermercado · pizzaria · auto peças · mecânica · escola · hotel · pousada · sorveteria · padaria · pet shop

---

## Estratégia de conteúdo

- **Corpo do email = mesmo por serviço**, variando **assunto + headline + bloco de dor** por nicho.
- **Um serviço por vez, por nicho**, espaçado (não 4 emails no mesmo dia ao mesmo lead).
- **Variáveis DIAX:** `{{empresa}}`, `{{cidade}}`, `{{nome}}`, `{{cta_link}}`, `{{unsubscribe_url}}`.
- **CTA primário:** WhatsApp (maior resposta no BR) · **secundário:** site/portfólio.
- **Imagens (economia):** 4 banners-hero (1 por serviço), reutilizados em todos os nichos. Personalização do nicho é textual. Gerar via FAL.AI (Flux) ou Canva MCP.

---

## Fases

### Fase 0 — Fundação & deliverability (GATE — antes de qualquer envio)
- [ ] Confirmar SPF/DKIM/DMARC do domínio `alexandrequeiroz.com.br` no Brevo
- [ ] Confirmar remetente `contato@alexandrequeiroz.com.br` verificado no DIAX/Brevo
- [ ] Subir logo AQ no DIAX CRM (pedido do usuário)
- [ ] Definir limites de warmup (começar 20–50/dia)

### Fase 1 — Pipeline de leads
- [ ] Sincronizar Extrator → DIAX (caminho existente `/api/leads/send-to-crm`)
- [ ] Mapear `category` (nicho) do Extrator → `tags` do DIAX customers
- [ ] Recuperar contatos/emails de campanhas passadas no DIAX, mesclar sem duplicar
- [ ] Garantir filtro de destinatário por tag de nicho (verificar/ajustar endpoint de recipientes)

### Fase 2 — Criativos
- [ ] 4 banners-hero (1 por serviço) — FAL.AI/Canva, com logo + cor `#57b3df`
- [ ] 4 templates HTML email-safe (frontend-design) com variáveis + rodapé AQ + unsubscribe
- [ ] 3 variações de assunto por nicho/serviço (otimizador IA do DIAX)
- [ ] Copy final com skill `cold-email` / `copywriting`

### Fase 3 — Piloto (1 nicho + 1 serviço)
- [ ] **Recomendado: restaurante + Sites** (necessidade universal, fácil de vender)
- [ ] Volume baixo (~50–100), enviar, medir open/click/bounce/spam por 48h
- [ ] Ajustar copy/assunto conforme resultado

### Fase 4 — Escala controlada
- [ ] Expandir nicho a nicho, serviço a serviço, respeitando limites de provedor e anti-spam
- [ ] Acompanhar conversões no relatório de campanha do DIAX

---

## Guardrails (recomendações dentro do escopo)
1. **Warmup do domínio** — subir volume gradual; cold email em massa queima o domínio.
2. **Unsubscribe + supressão (LGPD)** — obrigatório em todo template (DIAX já tem).
3. **Validar emails antes** (Extrator tem `validate_email_free`) para reduzir bounce.
4. **WhatsApp como CTA** — botão de resposta direta no rodapé.

---

## Referências de código (não reconstruir)
- DIAX backend: `api-core/src/Diax.Application/EmailMarketing/EmailMarketingService.cs`
- DIAX outreach/segmentação: `api-core/src/Diax.Application/Outreach/OutreachService.cs`
- DIAX worker de fila: `api-core/src/Diax.Infrastructure/Email/EmailQueueProcessorWorker.cs`
- DIAX webhook tracking: `api-core/src/Diax.Api/Controllers/V1/BrevoWebhookController.cs`
- DIAX templates engine: `api-core/src/Diax.Application/EmailMarketing/EmailTemplateEngine.cs`
- Extrator leads/schema: `app/backend/app.py` (CREATE TABLE leads ~linha 2035)
- Extrator envio: `app/backend/email_campaigns.py` · provedores: `app/backend/email_providers.py`
