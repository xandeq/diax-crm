# -*- coding: utf-8 -*-
"""Infra do motor de cadencia (SCRP-01, Phase 10).

Extraido VERBATIM de fire_today_20.py — auth, providers, busca de leads, geracao de imagem e
circuit breaker. Consumido por daily_waves.py (motor) e pelos utilitarios de scraping/import.

HEALTHY, PER_PROVIDER e assign_providers vivem JUNTOS de proposito: daily_waves.refresh_healthy()
faz `ft.HEALTHY[:] = healthy` (slice-assign in-place) e assign_providers le HEALTHY dos globals
deste modulo. Separar os dois dessincroniza em silencio. Ver waves_smoke.py check 4.

Funcoes PURAS do orquestrador continuam em waves_lib.py — nao duplicar nada de la aqui.
"""
import os, sys, re, json, time, datetime, io, base64, math, urllib.parse, requests
from pathlib import Path

DIR = str(Path(__file__).resolve().parent) + '/'
base = 'https://api.alexandrequeiroz.com.br'
HEALTHY = ['Brevo', 'Mailjet', 'Resend', 'SendGrid']
PER_PROVIDER = 5  # 5 x 4 = 20 por segmento; 4 segs = 20/provider/hora (<=22 buffer)

# ---------- Warmup de volume (EMAL-02, Phase 12) ----------
# D-03 (decisão travada): 87 -> 300/dia em +20%/dia, ~8 dias úteis. Ritmo padrão de warmup de
# deliverability — NÃO é negociável pra mais rápido. A curva é literal de propósito: um
# `int(87*1.2**n)` daria 87,104,125,150,180,215,259,311 e estouraria o teto de 300.
WARMUP_TARGETS = [87, 104, 125, 150, 180, 216, 260, 300]
WARMUP_KEEP_RATIO = 0.80   # entregou menos que isso do alvo? segura o degrau (warmup honesto)
PER_PROVIDER_WAVE_CAP = 40 # teto por provider por wave/hora; backend HourlyLimit=225 é o hard stop
SEGS_PER_WAVE = 4          # daily_waves.build_waves(): wave_idx = i // 4


def warmup_target(day):
    """Alvo de volume do dia N do warmup (1-indexed), clampado em [87, 300]."""
    idx = min(max(day, 1), len(WARMUP_TARGETS)) - 1
    return WARMUP_TARGETS[idx]


def max_per_seg(target, n_seg=12, n_prov=4, min_seg=4):
    """Quantos leads buscar por segmento pra somar ~`target`/dia, dividido pelos `n_seg`
    segmentos do plano. Clamp superior por provider: hoje PER_PROVIDER=5 x 4 segs/wave =
    20/provider/wave (nota original "<=22 buffer"); no alvo 300 isso sobe a 25/provider/wave,
    ainda dentro do novo teto de 40 e bem abaixo do HourlyLimit=225 do backend. Clamp inferior
    por `min_seg` — nunca justifica menos que isso por nicho. Próximo gargalo, depois desta
    fase, é a cota diária de free tier por provider (300/dia ÷ 4 providers = 75/provider/dia).
    """
    per_seg = math.ceil(target / max(n_seg, 1))
    cap = PER_PROVIDER_WAVE_CAP * n_prov / SEGS_PER_WAVE
    return int(max(min(per_seg, cap), min_seg))


def next_warmup_day(dia, entregue, alvo):
    """Avança 1 degrau se entregou >= WARMUP_KEEP_RATIO do alvo; senão segura o degrau atual.
    Nunca ultrapassa o último dia da curva (teto 300)."""
    if alvo > 0 and entregue >= alvo * WARMUP_KEEP_RATIO:
        return min(dia + 1, len(WARMUP_TARGETS))
    return dia

FREE_WEBMAIL = {
    'gmail.com', 'hotmail.com', 'outlook.com', 'yahoo.com', 'yahoo.com.br', 'hotmail.com.br',
    'outlook.com.br', 'live.com', 'icloud.com', 'bol.com.br', 'uol.com.br', 'terra.com.br',
    'ig.com.br', 'globo.com', 'globomail.com', 'msn.com', 'aol.com', 'gmail.com.br', 'me.com',
    'yahoo.com.mx', 'r7.com', 'zipmail.com.br', 'oi.com.br', 'pop.com.br'
}
FOREIGN_TLD = ('.pt', '.es', '.us', '.ar', '.uk', '.fr', '.it', '.de', '.mx', '.py',
               '.uy', '.cl', '.bo', '.pe', '.ve', '.co', '.ca', '.au', '.jp', '.cn', '.ru')

def ts():
    return datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=-3))).strftime('%H:%M:%S')

def load_env():
    for line in open('C:/Users/acq20/.claude/.secrets.env', encoding='utf-8'):
        line = line.strip()
        if line and not line.startswith('#') and '=' in line:
            k, v = line.split('=', 1); os.environ[k] = v

def login():
    r = requests.post(base + '/api/v1/auth/login',
                      json={'email': os.environ['DIAX_ADMIN_EMAIL'], 'password': os.environ['DIAX_ADMIN_PASSWORD']},
                      timeout=20).json()
    tok = r.get('accessToken') or r.get('token')
    return {'Authorization': 'Bearer ' + tok, 'Content-Type': 'application/json'}

def qp(s):
    return urllib.parse.quote(s)

_REF_CAMPAIGN = '3dfba825-3980-4fdd-a5fc-4639b69fb0c1'

def ensure_breaker_closed(H, ref_campaign=_REF_CAMPAIGN):
    """Self-healing: se o circuit breaker estiver aberto, reseta via o endpoint admin."""
    try:
        ps = requests.get(base + '/api/v1/email-campaigns/campaigns/' + ref_campaign + '/pilot/status', headers=H, timeout=20).json()
        if not ps.get('isCircuitBreakerOpen'):
            return True
        print(f'[{ts()}] breaker ABERTO ({ps.get("circuitBreakerReason")}) -> auto-reset via API...')
        rr = requests.post(base + '/api/v1/email-campaigns/pilot/reset', headers=H, timeout=20)
        if rr.status_code == 200:
            print(f'[{ts()}] reset OK: {rr.text[:120]}')
            return True
        print(f'[{ts()}] reset FALHOU ({rr.status_code}): {rr.text[:160]}')
        return False
    except Exception as e:
        print(f'[{ts()}] ensure_breaker_closed erro: {e}')
        return False

def build_html(hook, img1, img2, intro, bullets, wa):
    bl = ''.join(
        '<p style="margin:0 0 8px;font-size:15px;color:#0f172a;">&#8250; ' + b + '</p>'
        for b in bullets
    )
    return (
        '<!DOCTYPE html><html><head><meta charset="utf-8"/>'
        '<meta name="viewport" content="width=device-width,initial-scale=1.0"/></head>'
        '<body style="margin:0;padding:0;background:#eef2f5;">'
        '<table width="100%" cellpadding="0" cellspacing="0" style="background:#eef2f5;">'
        '<tr><td align="center" style="padding:20px 8px;">'
        '<table width="100%" cellpadding="0" cellspacing="0" '
        'style="max-width:600px;background:#fff;border-radius:12px;overflow:hidden;">'
        '<tr><td style="background:#0f172a;padding:18px 26px;">'
        '<img src="https://www.alexandrequeiroz.com.br/images/logo.png" '
        'alt="AQ" width="150" style="display:block;"/></td></tr>'
        '<tr><td style="height:3px;background:#57b3df;font-size:0;">&nbsp;</td></tr>'
        '<tr><td style="padding:24px 26px 14px;">'
        '<h1 style="margin:0;font-family:Arial,sans-serif;font-size:22px;'
        'line-height:1.3;color:#0f172a;">' + hook + '</h1></td></tr>'
        '<tr><td><img src="' + img1 + '" width="600" alt="" '
        'style="display:block;width:100%;"/></td></tr>'
        '<tr><td style="padding:20px 26px;font-family:Arial,sans-serif;">'
        '<p style="margin:0 0 12px;font-size:15px;line-height:1.6;color:#334155;">'
        + intro + '</p>'
        '<p style="margin:0 0 10px;font-size:15px;font-weight:bold;color:#0f172a;">'
        'Para a {{empresa}}:</p>'
        + bl +
        '</td></tr>'
        '<tr><td align="center" style="padding:16px 26px;">'
        '<a href="https://wa.me/5527999840101?text=' + qp(wa) + '&amp;utm_source=email&amp;utm_medium=cold_email&amp;utm_campaign=aq" '
        'target="_blank" style="display:inline-block;background:#57b3df;color:#fff;'
        'padding:13px 26px;font-family:Arial,sans-serif;font-size:16px;'
        'font-weight:bold;text-decoration:none;border-radius:8px;">'
        'Diagn&oacute;stico gratuito pelo WhatsApp &#8250;</a></td></tr>'
        '<tr><td style="padding:12px 26px 0;">'
        '<img src="' + img2 + '" width="548" alt="" '
        'style="display:block;width:100%;border-radius:6px;"/></td></tr>'
        '<tr><td style="background:#0f172a;padding:16px 26px;text-align:center;">'
        '<p style="margin:0 0 4px;font-family:Arial,sans-serif;font-size:12px;color:#94a3b8;">'
        'Alexandre Queiroz &middot; (27) 99984-0101 &middot; alexandrequeiroz.com.br</p>'
        '<p style="margin:0;font-family:Arial,sans-serif;font-size:11px;color:#475569;">'
        'Vit&oacute;ria, ES &middot; '
        '<a href="{{unsubscribe_url}}" style="color:#475569;">descadastrar</a>'
        '</p></td></tr>'
        '</table></td></tr></table></body></html>'
    )

def gen_image(prompt, H, keyword=None):
    """Pollinations (3 retries, random seed) -> Pexels fallback -> PIL 600px JPG -> upload /email-images -> hosted PublicUrl."""
    from PIL import Image
    import random

    def _download_and_upload(raw_bytes):
        im = Image.open(io.BytesIO(raw_bytes)).convert('RGB')
        w = 600; h = int(im.height * (w / im.width))
        im = im.resize((w, h), Image.LANCZOS)
        buf = io.BytesIO(); im.save(buf, 'JPEG', quality=82, optimize=True)
        b64 = base64.b64encode(buf.getvalue()).decode()
        up = requests.post(base + '/api/v1/email-images/upload', headers=H, timeout=60,
                           json={'FileName': 'aq20.jpg', 'Base64Content': b64, 'ContentType': 'image/jpeg'})
        j = up.json()
        return j.get('publicUrl') or j.get('PublicUrl')

    # Try Pollinations with 3 different seeds
    for attempt in range(3):
        try:
            seed = random.randint(1, 99999)
            url = ('https://image.pollinations.ai/prompt/' + qp(prompt)
                   + f'?width=1024&height=640&model=flux&nologo=true&seed={seed}')
            raw = requests.get(url, timeout=120).content
            return _download_and_upload(raw)
        except Exception as e:
            print(f'   Pollinations attempt {attempt+1} falhou: {type(e).__name__}: {e}')
            time.sleep(2)

    # Fallback: Pexels stock photo
    try:
        pexels_key = os.environ.get('PEXELS_API_KEY', '')
        q = keyword or prompt.split(',')[0][:50]
        resp = requests.get('https://api.pexels.com/v1/search',
                            params={'query': q, 'orientation': 'landscape', 'per_page': 5, 'size': 'large'},
                            headers={'Authorization': pexels_key}, timeout=20).json()
        photos = resp.get('photos', [])
        if photos:
            img_url = photos[0]['src']['large']
            raw = requests.get(img_url, timeout=60).content
            return _download_and_upload(raw)
    except Exception as e:
        print(f'   Pexels fallback falhou: {e}')

    return None

# mode='corp' (default, INALTERADO): os 20 nichos não passam `mode`, então nada muda pra eles.
# mode='webmail' (EMAL-04, Phase 12 plano 04): inverte o portão FREE_WEBMAIL (exclusivo, não
# aditivo — webmail é população EM TESTE, com cap e relatório próprios; misturar com corp
# destruiria a capacidade de medir a taxa de resposta dela separadamente, que é o que EMAL-04
# pede) e pula a checagem de dup-dominio (senão o 2º gmail do dia morreria como duplicado, já
# que muitos leads webmail compartilham o mesmo provedor de email).
def email_ok(email, seen_emails, seen_domains, mode='corp'):
    if mode not in ('corp', 'webmail'):
        raise ValueError(f"mode invalido: {mode!r} (esperado 'corp' ou 'webmail')")
    if not email or '@' not in email:
        return False, 'sem-email'
    email = email.strip().lower()
    if '%' in email:
        return False, 'email-malformado'  # URL-encoded chars (%20 etc)
    dom = email.split('@', 1)[1]
    if email in seen_emails:
        return False, 'dup-email'
    if mode == 'corp':
        if dom in seen_domains:
            return False, 'dup-dominio'
        if dom in FREE_WEBMAIL:
            return False, 'webmail-nao-corp'
    else:  # mode == 'webmail'
        if dom not in FREE_WEBMAIL:
            return False, 'nao-webmail'
    if any(dom.endswith(t) for t in FOREIGN_TLD):
        return False, 'tld-estrangeiro'
    return True, 'ok'

_MX_CACHE = None


def _mx_deliverable(domain):
    """Domínio lixo/placeholder ou sem MX/A -> não enfileira (cache 30d, salvo no exit)."""
    global _MX_CACHE
    try:
        import atexit, mx_check
        if _MX_CACHE is None:
            _MX_CACHE = mx_check.load_cache()
            atexit.register(lambda: mx_check.save_cache(_MX_CACHE))
        return mx_check.deliverable_domain(domain, _MX_CACHE)
    except Exception:
        return True   # falha da checagem nunca bloqueia o envio


def fetch_leads(seg_cfg, H, seen_emails, seen_domains, seen_company, want, mode='corp'):
    """Busca leads do CRM por nicho, filtra corporativo+nunca+BR+dedup.
    mode='webmail' (EMAL-04): caminho paralelo pro cap/tag de webmail (build_webmail_wave em
    daily_waves.py) — não adiciona o domínio a seen_domains (senão o 2º gmail do dia morreria)."""
    picked = []
    page = 1
    examined = 0
    while len(picked) < want and page <= 8:
        r = requests.get(base + '/api/v1/leads',
                         params={'search': seg_cfg['search'], 'page': page, 'pageSize': 100},
                         headers=H, timeout=30).json()
        items = r.get('items', [])
        if not items:
            break
        for it in items:
            examined += 1
            if it.get('emailOptOut'):
                continue
            if (it.get('emailSentCount') or 0) > 0 or it.get('lastEmailSentAt'):
                continue  # nunca-contatado
            email = (it.get('email') or '').strip().lower()
            ok, _ = email_ok(email, seen_emails, seen_domains, mode=mode)
            if not ok:
                continue
            # MX/placeholder antes de enfileirar: bounce 7,5% na semana 04/09 (instagram.local, wixpress)
            if not _mx_deliverable(email.split('@', 1)[1]):
                continue
            comp = (it.get('companyName') or it.get('name') or '').strip().lower()
            comp_key = re.sub(r'[^a-z0-9]', '', comp)
            if comp_key and comp_key in seen_company:
                continue
            seen_emails.add(email)
            if mode == 'corp':
                seen_domains.add(email.split('@', 1)[1])
            if comp_key:
                seen_company.add(comp_key)
            picked.append({'id': it['id'], 'email': email,
                           'company': it.get('companyName') or it.get('name'),
                           'website': (it.get('website') or '').strip(),
                           'score': it.get('leadScore') or 0})
            if len(picked) >= want:
                break
        if not r.get('hasNextPage'):
            break
        page += 1
    return picked, examined

def assign_providers(leads):
    for i, ld in enumerate(leads):
        ld['assignedProvider'] = HEALTHY[i % len(HEALTHY)]
    return leads
