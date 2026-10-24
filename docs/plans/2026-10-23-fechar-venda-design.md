# Fechar venda — design (aprovado 23/10/2026)

## Problema

O CRM gera contato, mas não acompanha a venda.

- 45 dias: 738 emails (~0,1% de resposta humana) e 553 WhatsApps (~34% respondem, incluindo autoresposta).
- Nenhum lead passou de "Qualificado". Zero propostas desde 03/07. Pipeline com R$0.
- O bot do WhatsApp (`n8n-local/workflows/waha-bot-atendente.json`) responde só a mensagem atual, sem histórico, sem saber quem é o lead e sem objetivo. Em pergunta de preço ou prazo diz "o Alexandre responde em seguida" e nada acontece no CRM.

Decisões do usuário: o gargalo é **fechar venda**; o passo que fecha é **proposta direto** (sem call); preço por **pacotes fixos**; abordagem **A em 2 fatias** (CRM primeiro, bot depois, sempre com revisão humana antes de enviar).

## O que já existe e será reaproveitado

- `Proposal` (`Diax.Domain/Customers/Proposal.cs`): Draft → Sent → Accepted → Paid / Cancelled, link público com token, aceite público, `mark-paid`, envio por email, `pix_key`, `view_count`.
- Pipeline: o próprio `Customer` é a oportunidade (`status`, `estimated_value`, `expected_close_date`); tela `crm-web/src/app/pipeline`.
- Padrão de worker diário idempotente com `job_runs` (#201).
- Evento de integração `POST /api/v1/integrations/whatsapp-event` com `X-Integration-Key`.

As tabelas legadas `Opportunities`, `Sales`, `Products` e `Leads` não têm código. Ficam como estão (remover é outra tarefa).

## Fatia 1 — CRM (sem IA)

1. **`service_packages`**: `name`, `short_description`, `scope` (lista de itens), `price`, `delivery_days`, `is_active`, `sort_order`, auditoria padrão. CRUD em Propostas → Pacotes. Começa com 3–4 pacotes aprovados pelo usuário.
2. **`proposals.package_id`** (opcional, FK) e **`proposals.origin`** (`manual` | `bot`).
3. **"Gerar proposta"** no card do lead e no pipeline: escolhe pacote, preenche título, descrição, valor e validade (7 dias). Nasce **Rascunho**; o usuário revisa e envia (email e/ou link para WhatsApp).
   - Gerar/enviar → cliente vai para **Negotiating** com `estimated_value` = valor da proposta.
   - Aceite → **Customer**. Pagamento → `paid_at`.
4. **Worker de follow-up** (diário, `job_runs`): cria tarefa quando
   - enviada e não visualizada há 48h → "reenviar ou chamar no WhatsApp";
   - visualizada e não aceita há 48h → "follow-up";
   - vencida → "renovar ou encerrar".
   Uma tarefa por proposta por etapa. Restart não duplica.
5. **Painel de receita** no dashboard comercial: R$ em proposta aberta, R$ aceito e pago no mês, taxa proposta → aceite, tempo resposta → proposta.

Fora da v1: gateway de pagamento, contrato/assinatura, itens adicionais por proposta.

## Fatia 2 — bot que conduz até o rascunho

**Entrada:** últimas 15 mensagens do chat (WAHA) + contexto do lead no CRM (nome, segmento, oferta do 1º contato, status) + catálogo via endpoint novo só leitura com `X-Integration-Key`. Casar lead por `customerId` da base de outreach; telefone só como último recurso (placeholder `0000000000` já casou errado).

**Objetivo:** descobrir necessidade, pacote e prazo. Pode dizer "a partir de R$X" **só a partir do catálogo**. Nunca desconto, link, preço fechado ou prazo fora do pacote.

**Saída (JSON):** `{reply, intent, package_id, pronto_para_proposta, resumo}`. Com `pronto_para_proposta = true`, o n8n chama `whatsapp-event {event:'proposal_draft', customerId, package_id, resumo}` e o CRM cria proposta Rascunho (`origin = bot`), move o cliente para Negotiating e cria tarefa urgente "Revisar e enviar proposta" com o resumo.

**Travas:**
- Mensagem manual do dono no chat → bot em silêncio 24h naquele chat.
- Máximo 6 respostas do bot por lead; depois passa para o dono com resumo.
- Guard de opt-out atual continua.
- No máximo um rascunho aberto por lead.

**Erros:**
- IA falhou (Groq → Gemini) → sem resposta genérica; cria tarefa e avisa o dono.
- CRM falhou ao criar rascunho → fila local com retry.
- Avisos passam pela caixa de saída de alertas com retry (itens #4/#10 do next-best-actions de 23/10).

**Ativação:** modo sombra primeiro (gera resposta e JSON, grava em log, não envia). O dono revisa 10–20 conversas; só então liga. O bot atual segue inalterado até lá.

## Testes

- API (xUnit): CRUD de pacote; proposta a partir de pacote preenche campos e move status; aceite → Customer; `proposal_draft` idempotente (1 rascunho aberto por lead); follow-up sem duplicar em restart.
- crm-web: build + teste da tela de pacotes e do "Gerar proposta".
- Bot: casos fixos (pergunta de preço, "quero site", recusa, mensagem manual do dono) em modo sombra contra JSON esperado.

## Métricas de sucesso

Propostas/semana, proposta → aceite (%), R$ aceito e pago no mês, tempo resposta → proposta. Base hoje: 0 propostas desde julho, R$0 no pipeline.

## Implantação

Fatia 1 em PRs pequenos — (a) pacotes, (b) gerar proposta + status, (c) follow-up, (d) painel — cada um com CI 6/6 e merge **só com ok do usuário** (merge na main = deploy). Fatia 2: sombra → revisão → ativação.

**Depende do usuário:** aprovar os 3–4 pacotes com preço (sugestão inicial a partir das propostas de julho, R$1.800–3.200).
