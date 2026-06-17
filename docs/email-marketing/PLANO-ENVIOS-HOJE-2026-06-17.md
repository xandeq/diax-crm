# Plano de envios — 2026-06-17

**Remetente:** contato@alexandrequeiroz.com.br / Alexandre Queiroz Marketing Digital
**Providers saudáveis:** Brevo, Mailjet, Resend, SendGrid (ElasticEmail/MailerSend PROIBIDOS)
**Capacidade restante agora:** 95/dia e 25/h por provider → 380/dia, 100/h no total
**Já enviado hoje:** 20 (Clínica → Sites, 5/5/5/5) · impactados a excluir: 49 emails
**Método:** `queue-with-assignment` (5 leads por provider) · formato novo (gancho→img pessoa usando serviço→texto→CTA→img dia a dia→slogan→logo) · imagens via **Pollinations (grátis)**

## Regras aplicadas
- 1 serviço por segmento hoje (não múltiplos serviços ao mesmo lead — rule anti-fadiga)
- Só leads corporativos/nunca-contatados, deduplicados, sem opt-out, excluindo os 49 impactados
- send-test interno antes de cada lote real
- Warmup: ~96 emails no total do dia (curva saudável dia-2)

## Matriz de hoje (rotaciona os 4 serviços)

| # | Segmento | Serviço (melhor fit) | Qtd | Distribuição | Janela | Elegíveis |
|---|---|---|---|---|---|---|
| ✅ | Clínica | **Sites** (agendamento/Google) | 20 | 5/5/5/5 | feito 06:43 | 95 |
| 1 | Contabilidade | **Software sob demanda** (sistema de gestão) | 20 | 5/5/5/5 | lote 1 | 97 (93 corp) |
| 2 | Advocacia | **Sites** (captação/presença) | 20 | 5/5/5/5 | lote 2 | 64 (51 corp) |
| 3 | Academia | **Apps** (app treino/check-in) | 18 | 5/5/4/4 | lote 3 | 21 (todos corp) |
| 4 | Imobiliária | **Landing pages** (página por imóvel) | 18 | 5/5/4/4 | lote 4 | 22 |

**Total novo hoje: 76** (+20 feitos = 96 no dia). Por provider: ~24/100 diário, 5/25 por hora em cada lote → **dentro de todos os limites**.

## Fit serviço × segmento (por que cada um)
- **Contabilidade → Software sob demanda:** escritórios contábeis vivem de sistemas; oferta de sistema sob medida ressoa.
- **Advocacia → Sites:** captação de clientes começa no Google; site profissional + autoridade.
- **Academia → Apps:** app de treino/check-in/agenda é dor real e diferencial competitivo.
- **Imobiliária → Landing pages:** página de captura por imóvel/lançamento converte lead.

## Por lote (pipeline de execução)
1. Selecionar ≤20 leads do segmento (dedup email/domínio/empresa/telefone, corporativo+nunca, excluir impactados).
2. Gerar 2 imagens via Pollinations: (a) pessoa do segmento usando o serviço; (b) dia a dia do segmento.
3. Montar email no formato novo + copy comercial do serviço + assunto personalizado {{empresa}}.
4. Criar draft → send-test admin@ → validar render/links/unsubscribe.
5. `queue-with-assignment` 5/provider → monitorar (sent/delivered/bounce).
6. Espaçar ~1h entre lotes (deliverability; limites já comportam tudo junto, mas warmup pede calma).

## Reserva / próximos dias (não hoje)
- Pet shop (27), Padaria (15), Dentista (10), Farmácia (8), Restaurante (só 7 restantes) → distribuir nos próximos dias com os serviços restantes (ex: Pet→Apps agendamento, Padaria→Landing, Restaurante→Apps).
- Clínica/Contabilidade/Advocacia têm volume para 2ª onda com OUTRO serviço em dia diferente.

## Critérios de parada (todos os lotes)
>2 bounces · qualquer spam · falha unsubscribe/tracking · erro repetido de provider · failed rate >5%.
