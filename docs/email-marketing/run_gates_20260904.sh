#!/usr/bin/env bash
# Gates de 04/09/2026 — ações que o auto mode do Claude Code não executa (DNS prod, DB prod,
# push em repo público, secret no host). Rodar no Git Bash:  bash docs/email-marketing/run_gates_20260904.sh [passo]
# Passos: dns | drafts | backfill | token | push | all   (default: all). Cada passo é idempotente.
set -euo pipefail
cd "$(dirname "$0")/../.."                     # raiz do diax-crm
SEC="$HOME/.claude/.secrets.env"
STEP="${1:-all}"
log(){ printf '\n\033[1;36m== %s ==\033[0m\n' "$*"; }
want(){ [ "$STEP" = all ] || [ "$STEP" = "$1" ]; }

# ---------- 1. SPF (21 lookups → 7) + DMARC p=quarantine pct=25 via WHM ----------
if want dns; then
  log "DNS via WHM (HostGator) — backup em docs/email-marketing/zone-backup-20260904.json"
  set -a; eval "$(grep -E '^WHM_(HOST|PORT|USER|API_TOKEN)=' "$SEC" | tr -d '\r')"; set +a
  B="https://${WHM_HOST}:${WHM_PORT}/json-api"; A="Authorization: whm ${WHM_USER}:${WHM_API_TOKEN}"
  # localiza as linhas atuais (não confiar em número fixo: a zona pode ter mudado)
  Z=$(curl -sk "$B/dumpzone?api.version=1&domain=alexandrequeiroz.com.br" -H "$A")
  SPF_LINE=$(printf '%s' "$Z" | python -c "import sys,json; r=[x for x in json.load(sys.stdin)['data']['zone'][0]['record'] if x.get('type')=='TXT' and x.get('name')=='alexandrequeiroz.com.br.' and str(x.get('txtdata','')).startswith('v=spf1')]; print(r[0]['Line'] if r else '')")
  DMARC_LINE=$(printf '%s' "$Z" | python -c "import sys,json; r=[x for x in json.load(sys.stdin)['data']['zone'][0]['record'] if x.get('type')=='TXT' and x.get('name')=='_dmarc.alexandrequeiroz.com.br.']; print(r[0]['Line'] if r else '')")
  echo "SPF line=$SPF_LINE  DMARC line=$DMARC_LINE"
  [ -n "$SPF_LINE" ] && curl -sk -G "$B/editzonerecord" -H "$A" --data-urlencode "api.version=1" \
    --data-urlencode "domain=alexandrequeiroz.com.br" --data-urlencode "line=$SPF_LINE" --data-urlencode "type=TXT" \
    --data-urlencode "name=alexandrequeiroz.com.br." --data-urlencode "class=IN" --data-urlencode "ttl=300" \
    --data-urlencode "txtdata=v=spf1 ip4:108.167.132.0/24 include:eig.spf.a.cloudfilter.net include:sendinblue.com include:spf.mailjet.com ~all" \
    | python -c "import sys,json; print('SPF:', json.load(sys.stdin)['metadata'])"
  [ -n "$DMARC_LINE" ] && curl -sk -G "$B/editzonerecord" -H "$A" --data-urlencode "api.version=1" \
    --data-urlencode "domain=alexandrequeiroz.com.br" --data-urlencode "line=$DMARC_LINE" --data-urlencode "type=TXT" \
    --data-urlencode "name=_dmarc.alexandrequeiroz.com.br." --data-urlencode "class=IN" --data-urlencode "ttl=14400" \
    --data-urlencode "txtdata=v=DMARC1; p=quarantine; pct=25; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r" \
    | python -c "import sys,json; print('DMARC:', json.load(sys.stdin)['metadata'])"
  echo "-- verificação no NS autoritativo:"
  nslookup -type=TXT alexandrequeiroz.com.br ns232.prodns.com.br 2>&1 | grep -i spf1 || true
  nslookup -type=TXT _dmarc.alexandrequeiroz.com.br ns232.prodns.com.br 2>&1 | grep -i dmarc1 || true
  echo "-- (em 1 semana: pct=25 → pct=100 no mesmo registro)"
fi

# ---------- 2. Cancelar 11 drafts órfãos de 03/09 (status 0 → 4, sem itens na fila) ----------
if want drafts; then
  log "Drafts órfãos 03/09 → Cancelled"
  PYTHONIOENCODING=utf-8 python - <<'EOF'
import sys; sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
import db; cx=db.connect(autocommit=False); cur=cx.cursor()
cur.execute("""UPDATE email_campaigns SET status=4, updated_at=SYSUTCDATETIME(), updated_by='daily_waves_cleanup_20260903'
 WHERE status=0 AND scheduled_at IS NULL AND name LIKE 'AQ - % - 2026-09-03 auto s%'
   AND NOT EXISTS (SELECT 1 FROM email_queue_items q WHERE q.campaign_id=email_campaigns.id)""")
print('drafts cancelados:', cur.rowcount); cx.commit(); cx.close()
EOF
fi

# ---------- 3. Backfill P1a (emailado→Contacted, clicou→Qualified) — evidência reversível ----------
if want backfill; then
  log "Backfill do funil (backfill_funnel_status.py — event_type=3 CORRIGIDO: 4 era bounce)"
  ( cd docs/email-marketing && PYTHONIOENCODING=utf-8 python -u backfill_funnel_status.py )
fi

# ---------- 4. Token de serviço: host do extrator + GitHub Secret do CRM (mesmo valor) ----------
if want token; then
  log "EXTRATOR_SERVICE_TOKEN — .secrets.env → host (/opt/extrator-api/.env) + gh secret EXTRATOR_API_TOKEN"
  TOK=$(grep -E '^EXTRATOR_SERVICE_TOKEN=' "$SEC" | head -1 | cut -d= -f2- | tr -d '"\r')
  if [ -z "$TOK" ]; then
    TOK=$(python -c "import secrets; print(secrets.token_urlsafe(48))")
    printf '\nEXTRATOR_SERVICE_TOKEN=%s\n' "$TOK" >> "$SEC"; echo "token novo gerado e salvo em .secrets.env"
  else echo "usando EXTRATOR_SERVICE_TOKEN já presente em .secrets.env"; fi
  SSH="ssh -i $HOME/.ssh/vps_hostinger_ed25519 -p 2282 -o IdentitiesOnly=yes root@185.173.110.180"
  $SSH "cd /opt/extrator-api && cp -a .env .env.bak_$(date +%Y%m%d_%H%M%S) && sed -i '/^EXTRATOR_SERVICE_TOKEN=/d' .env && printf 'EXTRATOR_SERVICE_TOKEN=%s\n' '$TOK' >> .env && chmod 600 .env && systemctl restart extrator-api && sleep 3 && systemctl is-active extrator-api && curl -s -o /dev/null -w 'health interno: %{http_code}\n' http://172.17.0.1:8000/api/health"
  echo "-- prova (401 sem token / 200 com token):"
  curl -s -o /dev/null -w 'sem token: %{http_code}\n' "http://185.173.110.180:8000/api/leads?page=1&page_size=1"
  curl -s -o /dev/null -w 'com token: %{http_code}\n' -H "Authorization: Bearer $TOK" "http://185.173.110.180:8000/api/leads?page=1&page_size=1"
  printf '%s' "$TOK" | gh secret set EXTRATOR_API_TOKEN -R xandeq/diax-crm
  gh secret set EXTRATOR_URL -R xandeq/diax-crm --body "http://185.173.110.180:8000"
  echo "-- GitHub Secrets atualizados (valem no próximo deploy)"
fi

# ---------- 5. Push + PR → merge → deploy automático (SmarterASP) ----------
if want push; then
  log "Push da branch + PR para main"
  git push -u origin chore/email-automation-versioned
  gh pr create -R xandeq/diax-crm --base main --head chore/email-automation-versioned \
    --title "feat(email): funil automático (P1b), waves tolerantes a atraso, FUP2/FUP3 c/ diagnóstico real, ExtractorPull ON" \
    --body "$(cat <<'MD'
Branch de automação de email versionada (cobre e substitui o PR #94).

- P1b: Lead→Contacted no envio real; →Qualified no clique (Brevo/Resend). Opens não contam.
- Waves tolerantes a atraso da task (incidente 03/09: 0 emails), resposta de schedule/queue verificada.
- FUP2/FUP3 multi-toque; FUP2 entrega diagnóstico real do site (site_check.py, SSRF-safe).
- ExtractorPullWorker ligado no deploy (`ExtractorPull.Enabled=true`, 15:00 UTC); config do extrator
  vem do GitHub Secret ANTES do AWS SM (rotação sem cofre).
- Testes: 684/684 .NET (Release) · 28 pytest offline.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
MD
)" || echo "(PR já existe?)"
  gh pr list -R xandeq/diax-crm --head chore/email-automation-versioned
  echo "-- Depois do CI verde: gh pr merge -R xandeq/diax-crm --squash --delete-branch=false <n>  → deploy api-core automático"
  echo "-- Verificar: gh run list -R xandeq/diax-crm -L 3 ; curl -s https://api.alexandrequeiroz.com.br/health"
fi
log "fim ($STEP)"
