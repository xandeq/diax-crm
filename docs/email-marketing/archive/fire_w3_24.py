# -*- coding: utf-8 -*-
"""Wave 3 — 2026-06-24 — escola x Software de Gestão + hotel x Apps."""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, gen_image, build_html,
    fetch_leads, assign_providers, ensure_breaker_closed,
)

WAVE_FILE = DIR + 'previews/wave-2026-06-24-w3.json'

SEGMENTS = [
    dict(
        seg='escola', search_terms=['escola', 'colegio', 'educacao', 'ensino'],
        service='Software de Gestão',
        hook='Um sistema de gestão pode simplificar matrículas, notas e comunicados da sua escola.',
        subject='Um sistema de gestão escolar sob medida para o {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Escolas que ainda usam planilhas perdem tempo com matrículas, boletins e comunicados — um sistema centralizado automatiza tudo isso e libera sua equipe para o que importa.',
        bullets=['Matriculas e cadastros de alunos online',
                 'Boletins e notas acessiveis para os pais',
                 'Comunicados e avisos por mensagem automatica',
                 'Controle financeiro de mensalidades integrado'],
        wa='Ola! Quero um diagnostico rapido de um sistema de gestao para minha escola',
        img1='A Brazilian school director using a custom school management system on a computer in a modern school office, realistic editorial photo',
        img2='A modern Brazilian school with students in a classroom and a teacher using technology, bright and professional environment, realistic editorial photo',
    ),
    dict(
        seg='hotel', search_terms=['hotel', 'pousada', 'hospedagem', 'resort'],
        service='Apps',
        hook='Um app mobile pode facilitar o check-in, pedidos de quarto e reservas diretas dos seus hóspedes.',
        subject='Um aplicativo de experiência do hóspede para o {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Hotéis que oferecem app próprio reduzem filas no check-in, recebem pedidos de quarto pelo celular e fidelizam hóspedes com promoções exclusivas.',
        bullets=['Check-in e check-out pelo celular',
                 'Pedidos de servico de quarto pelo app',
                 'Reservas diretas sem comissao de OTA',
                 'Notificacoes de promocoes e eventos do hotel'],
        wa='Ola! Quero um diagnostico rapido de um app para meu hotel',
        img1='A Brazilian hotel receptionist showing a hotel guest app on a tablet in a modern hotel lobby, realistic editorial photo',
        img2='A luxurious Brazilian hotel lobby with natural light and a welcoming ambiance, guests checking in, realistic editorial photo',
    ),
]

MAX_PER_SEG = PER_PROVIDER * len(HEALTHY)


def fetch_leads_multi(search_terms, H, seen_e, seen_d, seen_c, want):
    all_leads = []
    for term in search_terms:
        if len(all_leads) >= want:
            break
        leads, _ = fetch_leads({'search': term}, H, seen_e, seen_d, seen_c, want - len(all_leads))
        if leads:
            print(f'   search="{term}" -> {len(leads)} leads')
            all_leads.extend(leads)
    return all_leads[:want]


def run():
    load_env(); H = login()
    print(f'\n[{ts()}] ===== WAVE 3 -- 24/06 -- escola x SG + hotel x Apps =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-06-24', 'wave': 3, 'created': ts(), 'segments': []}

    for cfg in SEGMENTS:
        print(f'\n[{ts()}] --- {cfg["seg"].upper()} x {cfg["service"]} ---')
        leads = fetch_leads_multi(cfg['search_terms'], H, seen_e, seen_d, seen_c, MAX_PER_SEG)
        if not leads:
            print('   0 leads disponiveis -- pulando'); continue

        leads = assign_providers(leads)
        print(f'[{ts()}]   {len(leads)} leads selecionados')

        img1 = gen_image(cfg['img1'], H, keyword=cfg['seg'])
        img2 = gen_image(cfg['img2'], H, keyword=cfg['seg'])
        html = build_html(cfg['hook'], img1 or '', img2 or '', cfg['intro'], cfg['bullets'], cfg['wa'])

        H2 = login()
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-24 w3'
        future = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5))
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H2,
                           json={'name': camp_name, 'subject': cfg['subject'],
                                 'fromName': 'Alexandre Queiroz',
                                 'fromEmail': 'contato@alexandrequeiroz.com.br',
                                 'bodyHtml': html,
                                 'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=30)
        if cr.status_code not in (200, 201):
            print(f'   !!! CRIACAO FALHOU {cr.status_code}: {cr.text[:300]}'); continue
        cid = cr.json().get('id') or cr.json().get('campaignId')
        sc = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule',
                           headers=H2, json={'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=20)
        print(f'[{ts()}]   cid={cid[:8]} schedule={sc.status_code}')

        for ld in leads:
            ld['campaignId'] = cid
        wave['segments'].append({'seg': cfg['seg'], 'service': cfg['service'], 'campaignId': cid, 'leads': leads})

    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    total_built = sum(len(s['leads']) for s in wave['segments'])
    print(f'\n[{ts()}] Wave 3 BUILD: {total_built} leads')
    if total_built == 0:
        print('INVENTARIO ESGOTADO'); return

    print(f'\n[{ts()}] ===== WAVE 3 SEND =====')
    if not ensure_breaker_closed(H):
        print('[ABORT] circuit breaker aberto'); return
    H = login()

    for seg in wave['segments']:
        cid = seg['campaignId']
        payload = [{'customerId': ld['id'], 'assignedProvider': ld['assignedProvider']} for ld in seg['leads']]
        r = requests.post(base + '/api/v1/email-providers/queue-with-assignment',
                          headers=H, json={'campaignId': cid, 'leads': payload}, timeout=60)
        jr = r.json() if r.status_code in (200, 201, 202) else {}
        print(f'[{ts()}] {seg["seg"]:12} {seg["service"]:20} | QUEUE {r.status_code} queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | cid {cid}')

    print(f'[{ts()}] Wave 3 -- DONE')


if __name__ == '__main__':
    run()
