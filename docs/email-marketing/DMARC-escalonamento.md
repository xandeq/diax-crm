# DMARC — Escalonamento de Enforcement (EMAL-01)

Gerado em: 2026-09-20 (Phase 12, plano 12-05)

Escopo travado por `12-CONTEXT.md`: a ferramenta (`dmarc_escalation.py`, entregue no 12-01) existia
em dry-run; este plano provou o caminho de reversão em produção sob um registro descartável e passou
pelo primeiro gate humano de escrita real no `_dmarc`. **A decisão do usuário no checkpoint foi
`prova-e-segura`: provar a reversão (feito), mas segurar o degrau em `pct=25`** — o próprio limiar de
reversão travado em D-04 (bounce > 5%) está sendo violado agora, então subir o degrau contrariaria a
regra que o usuário mesmo definiu. Isto **não é uma falha desta fase** — é o mecanismo de segurança
funcionando como projetado.

---

## 1. Estado atual

**DNS ao vivo, consultado no NS autoritativo em 2026-09-20 (nesta execução, `nslookup -type=TXT
_dmarc.alexandrequeiroz.com.br ns232.prodns.com.br`):**

```
Server:  UnKnown
Address:  108.167.132.100

_dmarc.alexandrequeiroz.com.br	text =

	"v=DMARC1; p=quarantine; pct=25; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r"
```

**Zona WHM:** `pct=25` (Line 125 no momento desta execução — a Line se desloca conforme a zona muda;
nunca confiar num número fixo, sempre relocalizar via `dumpzone` antes de qualquer escrita, como
`set_pct()` já faz).

**State file (`previews/daily-state.json`) após esta execução:**

```json
"dmarc_pct": 25,
"dmarc_changed_at": "2026-09-20",
"dmarc_history": [
  {
    "date": "2026-09-20",
    "from": 25,
    "to": 25,
    "acao": "hold",
    "motivo": "hold: bounce=5.08% spam=0.00% em 59 envios (7d) acima do limiar bounce>5% (D-04). Decisao checkpoint 12-05 (prova-e-segura): usuario segura o degrau em pct=25, fiel a D-04, em vez de forcar avanco.",
    "health": {"sent": 59, "bounced": 3, "spam": 0, "bounce_rate": 0.0508, "spam_rate": 0.0}
  }
]
```

`dmarc_changed_at=2026-09-20` foi semeado de propósito **sem** nenhuma escrita de DNS — isso inicia o
relógio de 7 dias para que a primeira avaliação automática de subida (`--check`) só aconteça a partir
de 2026-09-27, em vez de o `decide()` continuar devolvendo "hold: sem `dmarc_changed_at`" para sempre.

## 2. A curva e a regra

Escalonamento travado: `25 → 50 → 75 → 100`, **1 degrau por semana** (D-01), gate de 7 dias contado a
partir de `dmarc_changed_at` no state — o DNS não guarda "quando mudou", só o state guarda (Pitfall 2
do 12-01).

Reverte se, na janela do degrau atual: `bounce_rate > 5%` **OU** `spam_rate > 0,1%` (D-04, travado
pelo usuário). Piso de amostra de 200 envios (`ADVANCE_MIN_SENT`) só bloqueia **avanço**, nunca
reversão — saúde ruim reverte mesmo com poucos envios.

Os números vêm de `email_events`, `event_type=4` (Bounced) e `event_type=5` (Spam), contados por
`COUNT(DISTINCT queue_item_id)` desde a janela do degrau atual — nunca de
`EmailCampaign.UnsubscribeCount`, que conflacia spam de verdade com descadastro voluntário
(`BrevoWebhookController.HandleOptOutAsync`, achado da pesquisa do 12-01).

## 3. Como rodar

```
python dmarc_escalation.py --status                  # leitura: pct da zona, state, saude, nslookup
python dmarc_escalation.py --check                    # roda decide(), imprime advance|hold|revert|done
python dmarc_escalation.py --advance                   # DRY-RUN da subida (nao escreve)
python dmarc_escalation.py --advance --apply            # ESCRITA REAL no DNS — exige aval humano
python dmarc_escalation.py --revert                     # DRY-RUN da reversao
python dmarc_escalation.py --revert --apply              # ESCRITA REAL de reversao
python dmarc_escalation.py --set-pct N [--apply]          # degrau explicito
python dmarc_escalation.py --drill [--apply]               # drill de reversao (registro descartavel)
```

**Só `--apply` escreve de verdade.** Qualquer `--apply` contra o `_dmarc` (via `--advance`,
`--revert` ou `--set-pct`) **exige aval humano explícito** — não é comando de rotina a ser
automatizado sem supervisão, dado o raio de explosão (todo email de saída do domínio).

## 4. Prova da reversão (Task 2, drill contra `_gsddrill`)

O caminho escrita→verificação→reversão→limpeza foi exercitado de ponta a ponta em produção contra
`_gsddrill.alexandrequeiroz.com.br.` — subdomínio descartável, sem nenhum serviço apontando para ele
— e **nunca** contra o `_dmarc` real. Log completo, em ordem:

1. `addzonerecord` TXT `_gsddrill...=gsd-drill-v1` → `metadata.result=1`.
2. `editzonerecord` → `gsd-drill-v2` (localizado por nome via `dumpzone`, não pela posição do add).
3. `nslookup -type=TXT _gsddrill.alexandrequeiroz.com.br ns232.prodns.com.br`, verbatim:
   ```
   Server:  UnKnown
   Address:  108.167.132.100

   _gsddrill.alexandrequeiroz.com.br	text =

   	"gsd-drill-v2"
   ```
4. **Reversão:** relocaliza a Line via `dumpzone` de novo, `editzonerecord` de volta para
   `gsd-drill-v1`.
5. `nslookup` pós-reversão, verbatim:
   ```
   Server:  UnKnown
   Address:  108.167.132.100

   _gsddrill.alexandrequeiroz.com.br	text =

   	"gsd-drill-v1"
   ```
   Esta é a mesma asserção que `set_pct(..., apply=True)` faz sobre o `_dmarc` real — prova que o par
   localizar→editar acerta o registro certo, e que a reversão de D-02 funciona.
6. `removezonerecord` pela Line relocalizada, seguido de `dumpzone` final: `_gsddrill` **não existe
   mais** na zona (`contains_gsddrill: false`).
7. Telegram de teste (`🧪 DMARC drill: reversão exercitada com sucesso em _gsddrill (nenhuma
   alteração no _dmarc)`) enviado com sucesso pelo mesmo `tg_send()` que a reversão real usaria.

**Fallback B não foi necessário** — `addzonerecord`/`removezonerecord` funcionaram na primeira
tentativa, sem exigir o caminho alternativo de reescrever o `_dmarc` com o próprio valor atual. (Esse
fallback continua implementado em `dmarc_escalation.py` — `--drill` cai nele automaticamente se
`addzonerecord` falhar — mas não foi acionado nesta execução.)

**Confirmação de que o `_dmarc` real nunca foi tocado durante o drill:** `nslookup` ao final ainda
mostra `pct=25`, byte a byte igual ao capturado antes do drill começar.

## 5. Saúde medida no dia (2026-09-20, re-medida nesta execução)

| Janela | enviados | bounces | spam | bounce rate | spam rate |
|--------|----------|---------|------|-------------|-----------|
| 7d  | 59  | 3  | 0 | 5,08%  | 0,00% |
| 14d | 260 | 27 | 0 | 10,38% | 0,00% |
| 30d | 936 | 99 | 0 | 10,58% | 0,00% |

**Leitura honesta:** mesmo na janela mais curta (7d), o bounce rate (5,08%) já está acima do limiar
de reversão de D-04 (>5%). `python dmarc_escalation.py --check` confirma:

```
acao=hold pct_atual=25 pct_alvo=25
motivo: ja no degrau minimo (pct=25) - bounce=5.08% spam=0.00% em 59 envios (limiar bounce>5% ou spam>0.1%)
```

O motor de decisão segura o degrau até a base melhorar — corretamente, pela própria regra do usuário.
A causa provável do bounce elevado é qualidade de lead na entrada (mesmo padrão de contaminação já
registrado nas auditorias das Phases 9 e 10, não uma falha do mecanismo de envio). Isto é **fora do
escopo desta fase** — candidato a fase futura de limpeza de lista/qualidade de entrada.

## 6. Cadência dos 3 degraus restantes

Os degraus `50→75→100` **não acontecem nesta sessão** — acontecem no calendário, um por semana,
começando na primeira avaliação em que a saúde permitir (a partir de 2026-09-27, dado o
`dmarc_changed_at` semeado nesta execução). Duas formas de operar, com recomendação:

- **(recomendada) Piggyback no ramo de sexta de `daily_waves.py main()`**, onde já vivem
  `weekly_report()`, `niche_refresh()` e `db_housekeeping()` — zero task nova no Task Scheduler, que é
  ponto frágil conhecido deste projeto (a Phase 10 descobriu ao vivo uma task que simplesmente não
  existia, ver `OBSERVABILIDADE-watchdogs.md` §2). O piggyback rodaria `python dmarc_escalation.py
  --check` e, se `advance`, `--advance --apply`, dentro do mesmo processo semanal já agendado.
- **(alternativa) Invocação manual semanal**: `python dmarc_escalation.py --check` e, se a saída for
  `advance`, `python dmarc_escalation.py --advance --apply` sob aval humano.

**O piggyback NÃO foi implementado neste plano** — `daily_waves.py` é território dos planos 12-02/
12-04, e ligar escrita automática de DNS no cron diário é decisão que merece seu próprio aval
explícito do usuário. Fica registrado aqui como a próxima decisão pendente, não como trabalho
concluído.

## 7. Limites e o que NÃO está coberto

- Não parseia o relatório DMARC agregado (`rua@dmarc.brevo.com` continua indo para o Brevo, leitura
  manual).
- Não mexe em SPF nem DKIM (fora do escopo de EMAL-01).
- TTL do registro `_dmarc` é 14400s — uma reversão real leva até ~4h para propagar em resolvers que
  já cachearam o valor anterior.
- Depende do notebook/sessão do usuário para rodar manualmente (mesma limitação documentada em
  `OBSERVABILIDADE-watchdogs.md` — nada migrou de máquina nesta fase).
- O token WHM vive só em `~/.claude/.secrets.env` (`WHM_HOST`/`WHM_PORT`/`WHM_USER`/`WHM_API_TOKEN`) —
  nunca commitado.
- **O degrau real permanece em `pct=25`** — o mecanismo de escrita/reversão está provado em produção,
  mas o avanço de enforcement em si (25→50→75→100) ainda não começou. Isso é uma decisão deliberada
  do usuário no checkpoint deste plano, não uma limitação técnica da ferramenta.

## 8. Como reverter manualmente, agora, se algo der errado

Comando único, copiável (reescreve `_dmarc` para o degrau mínimo, que é o mesmo valor de hoje —
seguro rodar mesmo sem incidente):

```
python dmarc_escalation.py --set-pct 25 --apply
```

Verificação obrigatória logo em seguida:

```
nslookup -type=TXT _dmarc.alexandrequeiroz.com.br ns232.prodns.com.br
```

Backup da zona do dia desta execução (antes de qualquer escrita real):
`docs/email-marketing/zone-backup-20260920.json`.

---

## Nota de manutenção

Este runbook é um snapshot de 2026-09-20. Quem retomar o escalonamento no futuro deve re-rodar
`python dmarc_escalation.py --status` e `--check` para confirmar que a leitura de saúde (§5) e a
decisão (§1/§5) ainda refletem a realidade — os números aqui vão ficar desatualizados a cada semana
que passa. O checkpoint humano documentado em `12-05-SUMMARY.md` (decisão `prova-e-segura`, verbatim)
é o registro de por que o degrau ficou em `pct=25` ao final desta fase, e continua válido até a
próxima avaliação de saúde mostrar bounce/spam dentro dos limiares de D-04.
