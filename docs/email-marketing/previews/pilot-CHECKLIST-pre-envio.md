# Checklist pré-envio — Piloto AQ · Restaurantes · Criação de Sites

**Campanha draft:** `AQ • Restaurantes • Criação de Sites • Piloto 2026-06`
**campaignId:** `94cb0f7e-e0bb-42c9-a955-aa504b781153`
**Status atual:** Draft (0 destinatários, 0 enviados) — NADA enviado.

## ✅ Já validado
- [x] Domínio autenticado no Brevo (DKIM brevo1/brevo2 + DMARC + brevo-code)
- [x] Remetente: contato@alexandrequeiroz.com.br / "Alexandre Queiroz Marketing Digital"
- [x] Template email-safe (tabelas + CSS inline), logo AQ (HTTP 200), cor #57b3df
- [x] CTA WhatsApp hardcoded (wa.me/5527999840101) — NÃO usa {{cta_link}} (que aponta p/ landing errada do DIAX)
- [x] {{cidade}} removido (renderiza vazio no DIAX)
- [x] {{empresa}} e {{nome}} populados no envio real (companyName/firstName)
- [x] {{unsubscribe_url}} presente (readiness gate exige) + injetado no envio
- [x] 29 leads elegíveis: deduplicados (email/domínio/empresa/telefone), email válido, sem opt-out
- [x] IDs salvos: previews/pilot-eligible-customer-ids.json
- [x] Supressão: backstop IsSuppressedAsync roda no envio; emailOptOut filtrado na seleção

## ⛔ Gates antes de QUALQUER envio (exigem decisão humana)
- [ ] **Aprovar o copy** (corpo) e **escolher 1 assunto** (3 variações em pilot-restaurante-sites-assuntos.md)
- [ ] **Decidir audiência do piloto:** 29 todos · ou só 12 nunca-contatados (mais seguro) · ou só os 17 já-contatados
- [ ] **Send-test interno** para 1 email do Alexandre (POST /campaigns/{id}/send-test) — AGUARDA confirmação explícita
- [ ] **Warmup:** confirmar volume baixo no 1º dia (sugiro ≤30; limite DIAX = 600/dia, 150/h)
- [ ] **Tagueamento** (aq-piloto-restaurante / aq-servico-sites / aq-campanha-2026-06): DEFERIDO — só há PUT de objeto completo (risco overwrite); fazer via UI ou endpoint aditivo, sob confirmação
- [ ] (Opcional) Banner-hero gerado (FAL.AI/Canva) — template já funciona sem ele

## Fluxo de envio quando autorizado (NÃO executar agora)
1. (confirmação) `POST /campaigns/94cb0f7e.../send-test` → 1 email interno p/ revisão de inbox
2. (confirmação) `POST /campaigns/94cb0f7e.../queue` com os 29 customerIds (ou subset) → enfileira
3. Worker envia respeitando limites; tracking via webhook Brevo (open/click/bounce/unsub)

## Observações
- unsubscribe aponta para domínio diaxcrm.com.br (endpoint da plataforma) — funcional.
- Sem cidade no copy → headline genérica (correto, pois city não existe no schema).
- Não inventar preço/oferta/estatística — copy atual não contém nenhum.
