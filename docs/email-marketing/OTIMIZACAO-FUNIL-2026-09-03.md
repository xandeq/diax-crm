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
- DMARC `p=none` → `p=quarantine` após validar alinhamento.
- SPF não inclui Resend (`include:_spf.resend.com` ou domínio de bounce).
- DKIM Brevo: seletor `mail._domainkey` vazio — validar seletor real (brevo1/brevo2).
- Rodar mail-tester.com num envio real p/ score spam.
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
