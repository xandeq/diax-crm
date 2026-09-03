# Roadmap de maximização do funil — 2026-09-03

Diagnóstico com dados reais (`funnel_analysis.py`, 30d). Priorizado por impacto/esforço.
Cada item marca se precisa de gate do usuário (mutação prod / DNS / deploy / config externa).

## Estado medido
| Métrica | Valor | Leitura |
|---|---|---|
| Volume | 87/900 dia | usa 10% da capacidade |
| Open rate | 2,5% (Brevo 6%, Resend 0%→corrigido) | baixo; deliverability + Apple MPP |
| Funil | 7611 Lead · **1 Contacted** · 2 Customer | **balde furado: não progride** |
| Cliques históricos | 60 | intenção real não convertida |
| Estoque | 2134 corp + 560 webmail | combustível ok; webmail ignorado |
| Bounce (30d) | 0 | lista limpa ✅ |

## Prioridade 1 — GESTÃO DO FUNIL (maior alavancagem, a venda vaza aqui)
**Problema**: 2243 leads já emailados seguem como "Lead" (status 0); 58 que **clicaram** estão sub-classificados. O CRM é uma lista, não um pipeline.
- **1a. Backfill** (`backfill_funnel_status.py`): emailado→Contacted (2243), clicou→Qualified (58). Evidência+reversível. ⛔ gate: mutação prod em massa (rodar com ok).
- **1b. Progressão automática** (código .NET, branch): worker marca Lead→Contacted ao enviar; webhook de click marca →Qualified. ⛔ gate: deploy.
- **1c. Hot lead → ação**: `sync_hot_to_crm` já cria tarefa (Urgent p/ reply, High p/ click). Agora que o Resend rastreia, cobre 100% dos envios. Sem gate (já ativo).
- **Impacto**: visibilidade real do funil + 58 clicadores viram follow-up ativo = vendas resgatadas.

## Prioridade 2 — DELIVERABILITY (subir open 2,5%→15-25%)
Verificado 03/09: **DKIM OK** nos dois (Brevo `brevo1`/`brevo2`, Resend `resend._domainkey`);
bounce 30d = 0 (não há rejeição em massa). Gaps reais:
- **DMARC `p=none`** → `p=quarantine` após validar alinhamento (hoje sem enforcement = receptores confiam menos).
- **SPF não inclui Resend** — confirmar Return-Path do Resend alinha (custom bounce domain) ou add ao SPF.
- Rodar mail-tester.com num envio real p/ score spam (parte do open baixo é Apple MPP mascarando, não spam).
- ⛔ gate: DNS de produção (Cloudflare). **Fazer ANTES de escalar volume.**

## Prioridade 3 — FOLLOW-UP MULTI-TOQUE (2-3x conversão)
Hoje só FUP1 (1 toque, 4-14d). Adicionar FUP2 (14-25d, quem não abriu FUP1) e FUP3 (breakup email). Código no `daily_waves.py` (`run_followups`). Sem gate (código na branch).

## Prioridade 4 — VOLUME (87 → 300-500/dia)
Estoque (2134) e cap (900) aguentam. Reavaliar os 560 webmail — PMEs BR usam gmail como email comercial; filtro atual descarta leads legítimos. ⚠️ Só após P2 (deliverability), senão amplifica spam. Warmup gradual (+50/dia/semana).

## Sequência recomendada
1. **P1c** já ativo (hot lead→tarefa, Resend rastreando).
2. **P2 deliverability** (destrava tudo — sem inbox, nada converte).
3. **P1a+1b** (gestão do funil — colher os que já responderam).
4. **P3 follow-up** (multiplicar toques).
5. **P4 volume** (escalar só com deliverability sã).

## Scripts prontos
- `funnel_analysis.py` — métricas do funil (rodar periodicamente).
- `backfill_funnel_status.py` — P1a (aguarda ok).

---
## Atualização 03/09 (noite) — o que foi feito e o que falta

### 🚨 Incidente do dia: 0 emails em 03/09
PC dormindo às 08:40 → task rodou **13:13 BRT** (`StartWhenAvailable`) → `/schedule` recusou
09:00 (já passado, 400) → o script ignorava a resposta → 11 campanhas ficaram **Draft** →
`queue-with-assignment` bloqueou pelo readiness gate (400) → **0 agendados**. Telegram avisou
"Agendados hoje: 0", mas sem a causa.
- **Fix** (`daily_waves.py` + `waves_lib.py`, 10 testes offline): slots tolerantes a atraso
  (slot no passado → agora+10min, gap 60min, corte às 17:00 BRT com alerta); resposta do
  `/schedule` e do `queue` agora é verificada e o corpo do 400 vai pro log; Telegram lista
  campanhas que falharam.
- **Task**: `WakeToRun=true` (backup `task-backup-pre-waketorun-20260903.xml`). Se o PC estiver
  em suspensão, acorda às 08:40; se desligado, roda ao ligar (com os slots novos).
- ⛔ **Gate**: 11 drafts órfãos de 03/09 (`AQ - * - 2026-09-03 auto sA`, sem itens) — cancelar
  (status=4) ou ignorar. UPDATE bloqueado em auto mode.

### ✅ P1b — progressão automática (commit `2b1d9754`, 683/683 testes)
Lead→Contacted no envio real (worker), Lead/Contacted→Qualified no clique (webhooks Brevo +
Resend). Opens não contam (Apple MPP). ⛔ Gate: push + deploy da branch.

### ✅ P3 — follow-up multi-toque (FUP2/FUP3)
FUP2 (7-21d após FUP1, ângulo "análise gratuita", cap 30) e FUP3 (7-21d após FUP2, breakup,
cap 20). Exclui: clicou, respondeu (IMAP), suprimido, opt-out, `status>=Qualified`, **caixas de
função** (`ouvidoria@`, `rh@`, `denuncia@`, `.gov.br/.org.br`…). Dry-run 03/09: 189 na janela,
43 excluídos, 30 saem amanhã. Estado em `daily-state.json` (`fup2_sent`/`fup3_sent`).

### 🔴 P2 — DELIVERABILITY: achado novo, maior que DMARC
**SPF faz 21 lookups DNS (limite RFC = 10) → PERMERROR.** Receptores tratam como SPF ausente;
DMARC hoje só passa por DKIM. `include:websitewelcome.com` sozinho custa 8; `sendinblue.com` 5.
Providers realmente usados (120d): Brevo (654), Resend (537), Mailjet (485, até 21/08),
Mailtrap (135, até 25/08), SendGrid (286, até 27/06 — morto). DKIM presente p/ Brevo, Resend,
Mailjet, SendGrid, ElasticEmail e HostGator (`default`). Resend usa `send.` (SPF próprio, não
precisa estar na raiz).

**Proposta (7 lookups)** — DNS fica no HostGator (`ns232/233.prodns.com.br`), editar via WHM:
```
v=spf1 ip4:108.167.132.0/24 include:eig.spf.a.cloudfilter.net include:sendinblue.com include:spf.mailjet.com ~all
```
(`ip4` = srv232 .96 + mail .104; `cloudfilter` = relay de saída da EIG/HostGator; SendGrid,
ElasticEmail, MailerSend, `a`, `mx` saem.) Validar depois com o mesmo script de contagem.

**DMARC** (após SPF sanear, 1 semana de intervalo):
```
v=DMARC1; p=quarantine; pct=25; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r
```
→ `pct=100` na semana seguinte. ⛔ Gate: DNS de produção.
