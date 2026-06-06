"""
run_campaign.py — Cria campanhas de email no DIAX CRM e enfileira leads.

Uso:
    python scripts/run_campaign.py [--config campaign-config.json] [--dry-run]

Config obrigatória (ver campaign-config.example.json):
    - api_url     : URL base da API (ou env DIAX_API_URL)
    - admin_email : email admin (ou env DIAX_ADMIN_EMAIL)
    - admin_password: senha (ou env DIAX_ADMIN_PASSWORD)
    - scheduled_at: ISO 8601 UTC (ou env DIAX_SCHEDULED_AT)
    - buckets     : lista de campanhas com leads por GUID

Precedência: variáveis de ambiente > config JSON > ~/.claude/.secrets.env
"""
import sys, io, json, os, argparse, urllib.request, urllib.error
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
from pathlib import Path

# ---------------------------------------------------------------------------
# Arg parsing
# ---------------------------------------------------------------------------
parser = argparse.ArgumentParser()
parser.add_argument('--config', default=None, help='Caminho para o arquivo de configuração JSON')
parser.add_argument('--dry-run', action='store_true', help='Simula sem criar campanhas')
args_cli = parser.parse_args()

# ---------------------------------------------------------------------------
# Load secrets from ~/.claude/.secrets.env (fallback)
# ---------------------------------------------------------------------------
def load_secrets_file() -> dict:
    path = Path.home() / '.claude' / '.secrets.env'
    if not path.exists():
        return {}
    out = {}
    for line in path.read_text(encoding='utf-8').splitlines():
        line = line.strip()
        if not line or line.startswith('#') or '=' not in line:
            continue
        k, _, v = line.partition('=')
        out[k.strip()] = v.strip().strip('"').strip("'")
    return out

SECRETS = load_secrets_file()

def cfg(key: str, fallback: str = '') -> str:
    return os.environ.get(key) or SECRETS.get(key) or fallback

# ---------------------------------------------------------------------------
# Load campaign config
# ---------------------------------------------------------------------------
config_path = args_cli.config
if config_path is None:
    default = Path(__file__).parent / 'campaign-config.json'
    if default.exists():
        config_path = str(default)

campaign_config: dict = {}
if config_path:
    with open(config_path, encoding='utf-8') as f:
        campaign_config = json.load(f)

def get(key: str, env_key: str | None = None, fallback: str = '') -> str:
    if env_key and os.environ.get(env_key):
        return os.environ[env_key]
    if key in campaign_config:
        return str(campaign_config[key])
    return cfg(env_key or key, fallback)

# ---------------------------------------------------------------------------
# Config resolution
# ---------------------------------------------------------------------------
API         = get('api_url',          'DIAX_API_URL').rstrip('/')
EMAIL       = get('admin_email',      'DIAX_ADMIN_EMAIL')
PASSWORD    = get('admin_password',   'DIAX_ADMIN_PASSWORD')
SCHEDULED   = get('scheduled_at',    'DIAX_SCHEDULED_AT', '2099-01-01T12:00:00Z')
BUCKETS     = campaign_config.get('buckets', [])

if not API or not EMAIL or not PASSWORD:
    sys.exit('[ERROR] Defina api_url, admin_email e admin_password no config JSON ou nas env vars.')

if not BUCKETS:
    sys.exit('[ERROR] Nenhum bucket definido. Configure "buckets" no campaign-config.json.')

# ---------------------------------------------------------------------------
# Auth
# ---------------------------------------------------------------------------
req = urllib.request.Request(
    f'{API}/api/v1/auth/login',
    data=json.dumps({'email': EMAIL, 'password': PASSWORD}).encode(),
    headers={'Content-Type': 'application/json'},
    method='POST',
)
with urllib.request.urlopen(req) as r:
    token = json.loads(r.read().decode())['accessToken']

auth = {'Authorization': f'Bearer {token}', 'Content-Type': 'application/json'}

# ---------------------------------------------------------------------------
# API helper
# ---------------------------------------------------------------------------
def api(method: str, path: str, body=None):
    data = json.dumps(body).encode('utf-8') if body is not None else None
    r = urllib.request.Request(f'{API}{path}', data=data, headers=auth, method=method)
    try:
        with urllib.request.urlopen(r, timeout=30) as resp:
            txt = resp.read().decode('utf-8')
            return resp.status, json.loads(txt) if txt else None
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode('utf-8')[:400]

# ---------------------------------------------------------------------------
# HTML wrapper
# ---------------------------------------------------------------------------
def html(body_text: str) -> str:
    paragraphs = [f'<p>{p.strip()}</p>' for p in body_text.strip().split('\n\n') if p.strip()]
    return '<div style="font-family:Arial,sans-serif;font-size:14px;line-height:1.5;color:#222;">' + '\n'.join(paragraphs) + '</div>'

# ---------------------------------------------------------------------------
# Run
# ---------------------------------------------------------------------------
total_leads = sum(len(b.get('leads', [])) for b in BUCKETS)
print(f'=== Campaigns: {len(BUCKETS)} | Total leads: {total_leads} | Scheduled: {SCHEDULED} ===')
if args_cli.dry_run:
    print('[DRY-RUN] Nenhuma ação executada.')
    for b in BUCKETS:
        print(f'  [{b["name"]}] {len(b.get("leads", []))} leads')
    sys.exit(0)

results = []
for b in BUCKETS:
    payload = {'name': b['name'], 'subject': b['subject'], 'bodyHtml': html(b.get('body', b.get('bodyText', '')))}
    st, body = api('POST', '/api/v1/email-campaigns/campaigns', payload)
    if st >= 400:
        print(f'[FAIL] Create "{b["name"]}": {st} {body}')
        results.append({'bucket': b['name'], 'status': 'create_failed', 'error': str(body)[:300]})
        continue

    cid = body.get('id') if isinstance(body, dict) else None
    if not cid:
        print(f'[FAIL] Create "{b["name"]}": no id in response. body={body}')
        continue
    print(f'[OK] Created campaign {cid} | {b["name"]}')

    leads = b.get('leads', [])
    if not leads:
        print('  [SKIP] No leads in bucket')
        continue

    q_payload = {'customerIds': leads, 'scheduledAt': SCHEDULED}
    st2, body2 = api('POST', f'/api/v1/email-campaigns/campaigns/{cid}/queue', q_payload)
    if st2 >= 400:
        print(f'  [FAIL] Queue: {st2} {body2}')
        results.append({'bucket': b['name'], 'campaignId': cid, 'status': 'queue_failed', 'error': str(body2)[:300]})
        continue

    print(f'  [OK] Queued {len(leads)} recipients @ {SCHEDULED}')
    results.append({'bucket': b['name'], 'campaignId': cid, 'recipients': len(leads), 'status': 'queued'})

print('\n=== SUMMARY ===')
print(json.dumps(results, ensure_ascii=False, indent=2))
