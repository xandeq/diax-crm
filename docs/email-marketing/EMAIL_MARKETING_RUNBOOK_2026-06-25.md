# DIAX CRM Email Marketing — Runbook 2026-06-25

Gerado em: 2026-06-25 (auditoria completa pré-execução)

---

## 1. ESTADO REAL DO SISTEMA

### Circuit Breaker
```
isCircuitBreakerOpen : false   ← FECHADO ✅
currentErrorRate     : 0
webhookFailureCount  : 0
campaignReadinessPassed: true
recentEvents         : []
```

Endpoint de verificação:
```
GET /api/v1/email-campaigns/campaigns/3dfba825-3980-4fdd-a5fc-4639b69fb0c1/pilot/status
```

Auto-reset via:
```
POST /api/v1/email-campaigns/pilot/reset
```

### Inventário de Leads (CRM ao vivo)
- Total leads: **3911**
- Leads com email: **3890**
- `emailSentCount` para todos: **0** ← ver seção RISCOS

### Status de 24/06
- 17 campanhas criadas, todas status=3 (sent/scheduled)
- Nichos/serviços enviados em 24/06:
  - clinica × Landing Page (w1)
  - contabilidade × Software de Gestão (w1)
  - loja × Landing Page (w2)
  - advocacia × App Mobile (w2)
  - escola × Software de Gestão (w3)
  - hotel × Apps (w3)
  - transportadora × Landing Page (w4)
  - restaurante × Sites (w4)
  - clinica × Software de Gestao (w5) ← **RISCO: ver seção abaixo**
  - loja × Apps (w5)
  - pet × Sites (w5)
  - contabilidade × Apps (w6)
  - advocacia × Sites (w6)
  - escola × Apps (w7)
  - hotel × Landing Page (w7)
  - loja × Sites (w8)
  - academia × Landing Page (w8)

---

## 2. INCONSISTÊNCIAS DETECTADAS (memória x realidade)

| Item | Memória | Realidade |
|------|---------|-----------|
| CB estado | "aberto (18/06)" | FECHADO — resolvido antes de 24/06 |
| 24/06 enviados | "345 leads (8 waves)" | 17 campanhas confirmadas via API |
| rescrape nichos | "transportadora/restaurante" | **ERRADO** — script alvo: estetica/autoescola/joalheria |
| fromName | "Alexandre Queiroz Marketing Digital" | Scripts w*_25 usam "Alexandre Queiroz" (sem Marketing Digital) |
| emailSentCount dedup | "filtro no fetch_leads" | INEFICAZ — campo nunca atualizado pelo backend |

---

## 3. AUDITORIA DOS SCRIPTS fire_w*_25.py

### Estrutura comum (todos w1-w8)
- ✅ `ensure_breaker_closed()` chamado antes de cada send — aborta se CB abrir
- ✅ Endpoint correto: `POST /api/v1/email-providers/queue-with-assignment`
- ✅ Dedup intra-wave: sets `seen_e`, `seen_d`, `seen_c` (email, domínio, empresa)
- ✅ Sem secrets no log — credenciais lidas de env vars
- ✅ Wave file JSON salvo em `previews/` antes do send (audit log)
- ✅ Aborts se inventário vazio para um segmento
- ✅ Login renovado (H2 = login()) antes de criar campanha
- ✅ Rate limit: PER_PROVIDER=5 × 4 providers = máx 20 emails/segmento
- ✅ Imagens: Pollinations (grátis) com fallback Pexels — sem custo
- ✅ fromName: 'Alexandre Queiroz' (consistente com 24/06 — veja nota abaixo)

### Mapa de waves 25/06
| Wave | Segmento 1 | Segmento 2 | Segmento 3 | Max leads |
|------|-----------|-----------|-----------|-----------|
| w1 | clinica × Apps | contabilidade × LP | — | 40 |
| w2 | advocacia × LP | escola × Sites | — | 40 |
| w3 | hotel × Sites | pet × LP | — | 40 |
| w4 | academia × Apps | clinica × Sites | — | 40 |
| w5 | contabilidade × Sites | escola × LP | pet × Apps | 60 |
| w6 | advocacia × SG | hotel × SG | — | 40 |
| w7 | academia × Sites | imobiliaria × LP | — | 40 |
| w8 | imobiliaria × Apps | clinica × SG [PER_PROVIDER=5] | — | 40 |
| **TOTAL** | | | | **~340** |

---

## 4. RISCOS IDENTIFICADOS

### ⚠️ RISCO CRÍTICO — Dedup entre dias não funciona
**Causa**: `emailSentCount` nunca é atualizado pelo backend após envio. O filtro em `fetch_leads` (`emailSentCount > 0`) avalia SEMPRE como False.

**Dedup que FUNCIONA**:
- Intra-wave: sets seen_e/seen_d/seen_c (apenas dentro do mesmo script)
- Intra-campanha: backend impede mesmo lead na mesma campanha (linha 1104 EmailMarketingService.cs)

**Dedup que NÃO FUNCIONA**:
- Cross-day: mesmo lead pode aparecer em 25/06 que já recebeu em 24/06
- Cross-wave: wave 2 pode pegar lead que estava em wave 1 (diferentes scripts, sets independentes)

**Impacto prático**: Leads de nichos repetidos (clinica, hotel, etc.) receberão múltiplos emails em dias consecutivos. Dentro do mesmo dia, cada wave tem sets fresh — possível duplicata entre w1 e w4 para clinica.

### ⚠️ RISCO MODERADO — clinica × SG duplicado (24/06 → 25/06)
- 24/06 w5: `AQ - clinica - Software de Gestao` → enviado
- 25/06 w8: `AQ - clinica - Software de Gestao` → MESMO serviço

Os mesmos leads de clinica podem receber "Software de Gestão" nos dois dias consecutivos.

**Opção 1**: Aceitar (email diferente com copy novo, baixo risco de spam)
**Opção 2**: Substituir w8 clinica×SG por nicho limpo (odontologia×SG, farmácia×SG)

### ⚠️ RISCO BAIXO — fromName inconsistência
Scripts 24/06 e 25/06 usam `fromName: 'Alexandre Queiroz'`.
Feedback de 2026-05-16 dizia usar `'Alexandre Queiroz Marketing Digital'`.
Como 24/06 já enviou com 'Alexandre Queiroz' sem problema, não é bloqueante.

### ℹ️ rescrape_21.py — nichos DIFERENTES da memória
- Memória dizia: "transportadora/restaurante" 
- Script real: **estetica/autoescola/joalheria**
- Transportadora e restaurante são SECOS por falta de leads no CRM, não por falta de scrape
- Rodar rescrape_21.py repõe estetica/autoescola/joalheria — nichos marginais
- Requer Chrome + Selenium instalados localmente — NÃO tem modo dry-run

---

## 5. CHECKLIST PRÉ-EXECUÇÃO

Antes de rodar QUALQUER wave:
- [ ] CB fechado: `curl .../pilot/status` → `isCircuitBreakerOpen: false`
- [ ] API respondendo: login retorna token
- [ ] Hora correta: rodar após 00:00 BRT (limites diários resetam)
- [ ] Não há wave do dia já rodando (verificar processos Python ativos)
- [ ] Verificar se w8 clinica×SG é aceitável ou quer trocar

---

## 6. SEQUÊNCIA DE EXECUÇÃO SEGURA

Rodar UMA wave por vez. Verificar output antes de próxima.

```bash
cd D:\claude-code\diax-crm\docs\email-marketing

# WAVE 1 (clinica×Apps + contabilidade×LP)
python fire_w1_25.py
# Verificar: QUEUE 200/202, queued>0, breaker=false

# WAVE 2 (advocacia×LP + escola×Sites)
python fire_w2_25.py

# WAVE 3 (hotel×Sites + pet×LP)
python fire_w3_25.py

# WAVE 4 (academia×Apps + clinica×Sites)
python fire_w4_25.py

# WAVE 5 (contabilidade×Sites + escola×LP + pet×Apps) — 3 segmentos, mais lenta
python fire_w5_25.py

# WAVE 6 (advocacia×SG + hotel×SG)
python fire_w6_25.py

# WAVE 7 (academia×Sites + imobiliaria×LP)
python fire_w7_25.py

# WAVE 8 [FINAL] (imobiliaria×Apps + clinica×SG)
# ATENÇÃO: clinica×SG duplicado com 24/06 — confirmar antes de rodar
python fire_w8_25.py
```

---

## 7. CHECKLIST PÓS-CADA WAVE

Verificar no output:
- `QUEUE 200` ou `202` → OK
- `queued=N` → confirmar N > 0
- `skip=M` → esperado (leads já contatados na campanha)
- `[ABORT] circuit breaker aberto` → PARAR TUDO, investigar
- `CRIACAO FALHOU` → PARAR, verificar API
- `INVENTARIO ESGOTADO` → nicho seco, pular

---

## 8. CRITÉRIOS DE PARADA IMEDIATA

Parar e investigar se:
- CB abrir (`isCircuitBreakerOpen: true`)
- Mais de 3 falhas de criação de campanha consecutivas
- `queued=0` em todos os segmentos por 2 waves seguidas (inventário esgotado)
- API retornar 5xx em mais de 2 tentativas
- Erro de autenticação (token expirou — fazer login manual para checar)

Não parar se:
- `skip=N` alto (normal — leads já processados nesta campanha)
- Imagem fallback para Pexels (OK, funciona)
- Um segmento retornar 0 leads (pula e continua)

---

## 9. RESCRAPE (rescrape_21.py)

**O que faz**: Scrape Google → estetica/autoescola/joalheria → importa no CRM
**Não tem dry-run** — roda ao vivo e importa direto com `dryRun: false`
**Requer**: Chrome instalado + selenium + webdriver_manager + scraper local
**Quando rodar**: SOMENTE após confirmar instalação de dependências
**Transportadora/restaurante**: NÃO são atingidos por rescrape_21.py (nichos secos por falta de leads, não de scrape)

Para rodar rescrape:
```bash
pip install selenium webdriver-manager
python rescrape_21.py
# Resultado em: scraper-result-2026-06-21.json
```

---

## 10. PRÓXIMAS AÇÕES RECOMENDADAS

Imediato (hoje 25/06):
1. **Decidir sobre w8 clinica×SG** — aceitar duplicata ou trocar nicho
2. Rodar waves w1→w7 (sequencial, verificando output)
3. Rodar w8 somente após decisão acima

Curto prazo:
4. Corrigir `emailSentCount` no backend — atualizar após `queue-with-assignment` sucesso
5. Implementar cooldown cross-day por customer (banco: `lastEmailSentAt` + min 3 dias)
6. Adicionar `fromName: 'Alexandre Queiroz Marketing Digital'` nos próximos scripts
7. Atualizar memória: rescrape_21.py é para estetica/autoescola/joalheria, NÃO transportadora

26/06 e além:
8. Criar fire_w*_26.py com nichos/serviços ainda não usados
9. Monitorar taxa de abertura e cliques via CRM analytics
10. Considerar pausa se bounce rate subir (transportadora/restaurante são indicadores ruins)

---

## APÊNDICE: Serviços por nicho (anti-fadiga tracker)

| Nicho | 24/06 services | 25/06 services | Pendentes |
|-------|----------------|----------------|-----------|
| clinica | LP, SG | Apps, Sites, SG* | E-commerce |
| contabilidade | SG, Apps | LP, Sites | E-commerce |
| advocacia | App Mobile, Sites | LP, SG | E-commerce |
| escola | SG, Apps | Sites, LP | Apps (novos) |
| hotel | Apps, LP | Sites, SG | E-commerce |
| pet | Sites | LP, Apps | Software |
| academia | LP | Apps, Sites | SG |
| loja | LP, Apps, Sites | — | todos |
| imobiliaria | — (21/06) | LP, Apps | Sites, SG |
| transportadora | LP | — (seco) | — |
| restaurante | Sites | — (seco) | — |

*SG = Software de Gestão
