# -*- coding: utf-8 -*-
"""Wave 11 — últimos combos remanescentes dos nichos das W1-W10:
  clinica×Landing, contabilidade×Apps, advocacia×Sites,
  hotel×Sites, escola×Landing, restaurante×Apps.
"""
import os, sys, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, ts, load_env, login,
    gen_image, build_html, fetch_leads, assign_providers, ensure_breaker_closed
)

WAVE_FILE = DIR + 'previews/wave-2026-06-20-w11.json'

SEGMENTS = [
    dict(
        seg='clinica', search_terms=['clinica', 'medica', 'saude'],
        service='Landing pages',
        hook='Uma landing page pode fazer pacientes encontrarem sua clínica no Google antes da concorrência.',
        subject='Uma landing page de captação de pacientes para a {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Pacientes pesquisam clínicas no Google — uma landing page objetiva com especialidades, convênios, depoimentos e WhatsApp para agendamento converte visitantes em pacientes todos os dias.',
        bullets=['Especialidades e equipe médica em destaque',
                 'Convênios e planos de saúde aceitos',
                 'Formulário de agendamento e WhatsApp para contato',
                 'Aparece no Google para quem busca clínica na cidade'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para minha clinica',
        img1='A Brazilian clinic waiting room with patients and a professional landing page shown on a screen, realistic editorial photo',
        img2='A modern Brazilian clinic reception with friendly staff and clean bright environment, realistic editorial photo',
    ),
    dict(
        seg='contabilidade', search_terms=['contabilidade', 'contador', 'contabil'],
        service='App Mobile',
        hook='E se seus clientes acompanhassem o resultado financeiro da empresa pelo celular em tempo real?',
        subject='Um app financeiro para os clientes da {{empresa}} ficarem sempre informados?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Escritórios de contabilidade com app próprio entregam relatórios financeiros, balancetes e DRE diretamente no celular dos clientes — diferencial competitivo que aumenta retenção e percepção de valor.',
        bullets=['DRE, balancete e fluxo de caixa no celular do cliente',
                 'Alertas de vencimentos fiscais e guias a pagar',
                 'Chat direto com o contador responsável',
                 'Documentos fiscais e notas em PDF no app'],
        wa='Ola! Quero um diagnostico rapido de um app financeiro para meu escritorio de contabilidade',
        img1='A Brazilian business owner checking financial reports on a accounting app on smartphone, realistic editorial photo',
        img2='A Brazilian accounting firm presenting mobile app features to a business client in a professional meeting, realistic editorial photo',
    ),
    dict(
        seg='advocacia', search_terms=['advocacia', 'advogado', 'juridico', 'adv'],
        service='Sites',
        hook='Seu escritório de advocacia aparece no Google quando alguém precisa de um advogado na sua cidade?',
        subject='Um site profissional para o escritório {{empresa}} que atrai clientes pelo Google?',
        intro='Olá! Sou o Alexandre Queiroz e crio sites. Quem precisa de advogado pesquisa no Google antes de ligar — um site com áreas de atuação, equipe, casos de sucesso e formulário de contato gera consultas novas todos os dias, sem depender só de indicação.',
        bullets=['Áreas de atuação e especialidades da equipe',
                 'Aparece no Google para quem busca advogado na cidade',
                 'Formulário de consulta e WhatsApp para atendimento',
                 'Blog jurídico para autoridade e SEO'],
        wa='Ola! Quero um diagnostico rapido de um site para meu escritorio de advocacia',
        img1='A Brazilian lawyer with a professional law firm website on a laptop in a modern office, realistic editorial photo',
        img2='A prestigious Brazilian law office reception with legal books and professional environment, realistic editorial photo',
    ),
    dict(
        seg='hotel', search_terms=['hotel', 'pousada', 'hospedagem', 'resort'],
        service='Sites',
        hook='Seu hotel aparece no Google quando alguém busca hospedagem na sua cidade?',
        subject='Um site profissional para o {{empresa}} receber reservas diretas pelo Google?',
        intro='Olá! Sou o Alexandre Queiroz e crio sites. Hóspedes pesquisam hotéis no Google antes de reservar — um site com fotos profissionais, quartos, tarifas e reserva direta pelo WhatsApp garante reservas sem pagar comissão para Booking ou Airbnb.',
        bullets=['Galeria de fotos profissionais dos quartos e áreas comuns',
                 'Aparece no Google para quem busca hotel na cidade',
                 'Reserva direta pelo WhatsApp sem intermediários',
                 'Tarifas, serviços e localização em destaque'],
        wa='Ola! Quero um diagnostico rapido de um site para meu hotel',
        img1='A hotel manager reviewing a professional hotel website on a laptop showing beautiful room photos, realistic editorial photo',
        img2='A beautiful Brazilian hotel pool area with well-maintained garden and professional hospitality staff, realistic editorial photo',
    ),
    dict(
        seg='escola', search_terms=['escola', 'colegio', 'educacao', 'ensino'],
        service='Landing pages',
        hook='Uma landing page pode fazer pais encontrarem sua escola no Google na época de matrícula.',
        subject='Uma landing page de captação de alunos para a {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Na época de matrículas, pais pesquisam escolas no Google — uma landing page com proposta pedagógica, infraestrutura, depoimentos e formulário de visita converte interessados em matrículas novas.',
        bullets=['Proposta pedagógica e diferenciais da escola',
                 'Aparece no Google para quem busca escola na cidade',
                 'Formulário de solicitação de visita ou matrícula',
                 'Depoimentos de pais e aprovações dos alunos'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para minha escola',
        img1='A Brazilian school principal presenting a school landing page on a laptop to interested parents, realistic editorial photo',
        img2='A modern Brazilian school building exterior with students and well-maintained campus, realistic editorial photo',
    ),
    dict(
        seg='restaurante', search_terms=['restaurante', 'lanchonete', 'churrascaria', 'pizzaria'],
        service='Apps',
        hook='E se seus clientes pedissem delivery e fizessem reservas direto pelo app do seu restaurante?',
        subject='Um app de pedidos para o {{empresa}} fidelizar clientes e aumentar o ticket médio?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Restaurantes com app próprio recebem pedidos de delivery, gerenciam mesas e enviam promoções diretamente para os clientes fiéis — sem pagar 30% de comissão para iFood ou Rappi.',
        bullets=['Cardápio digital com fotos e preços atualizados',
                 'Pedidos de delivery e retirada direto pelo app',
                 'Reserva de mesas e eventos pelo celular',
                 'Programa de fidelidade e cupons exclusivos para clientes'],
        wa='Ola! Quero um diagnostico rapido de um app de pedidos para meu restaurante',
        img1='A restaurant owner checking orders on a restaurant mobile app on a tablet at the counter, realistic editorial photo',
        img2='A modern Brazilian restaurant with customers dining and digital menu screens on the wall, realistic editorial photo',
    ),
]

MAX_PER_SEG = 5


def fetch_leads_multi(search_terms, H, seen_emails, seen_domains, seen_company, want):
    all_leads = []
    for term in search_terms:
        if len(all_leads) >= want:
            break
        leads, _ = fetch_leads({'search': term}, H, seen_emails, seen_domains, seen_company, want - len(all_leads))
        if leads:
            print(f'   search="{term}" → {len(leads)} leads')
            all_leads.extend(leads)
    return all_leads[:want]


def run():
    load_env(); H = login()
    print(f'\n[{ts()}] ===== WAVE 11 BUILD+SEND =====')
    os.makedirs(DIR + 'previews', exist_ok=True)
    seen_emails, seen_domains, seen_company = set(), set(), set()
    wave = {'date': '2026-06-20', 'wave': 11, 'created': ts(), 'segments': []}

    for cfg in SEGMENTS:
        print(f'\n[{ts()}] --- {cfg["seg"].upper()} × {cfg["service"]} ---')
        leads = fetch_leads_multi(cfg['search_terms'], H, seen_emails, seen_domains, seen_company, MAX_PER_SEG)
        if not leads:
            print(f'   0 leads — pulando'); continue
        leads = assign_providers(leads)
        print(f'[{ts()}]   {len(leads)} leads')
        img1 = gen_image(cfg['img1'], H, keyword=cfg['seg'])
        img2 = gen_image(cfg['img2'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img1={str(img1)[:55]} img2={str(img2)[:55]}')
        html = build_html(cfg['hook'], img1 or '', img2 or '', cfg['intro'], cfg['bullets'], cfg['wa'])
        H2 = login()
        future = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5)
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H2, timeout=30,
                           json={'name': f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-20 w11',
                                 'subject': cfg['subject'], 'fromName': 'Alexandre Queiroz',
                                 'fromEmail': 'contato@alexandrequeiroz.com.br', 'bodyHtml': html,
                                 'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')})
        if cr.status_code not in (200, 201):
            print(f'   !!! {cr.status_code}: {cr.text[:200]}'); continue
        cid = cr.json().get('id') or cr.json().get('campaignId')
        requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule',
                      headers=H2, json={'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=20)
        print(f'[{ts()}]   cid={cid[:8]}')
        for ld in leads: ld['campaignId'] = cid
        wave['segments'].append({'seg': cfg['seg'], 'service': cfg['service'], 'campaignId': cid, 'leads': leads})

    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    total_built = sum(len(s['leads']) for s in wave['segments'])
    print(f'\n[{ts()}] BUILD: {total_built} leads em {len(wave["segments"])} segs')
    if total_built == 0:
        print(f'[{ts()}] INVENTÁRIO ESGOTADO — DIA ENCERRADO'); return

    print(f'\n[{ts()}] ===== SEND =====')
    if not ensure_breaker_closed(H): print('[ABORT] breaker'); return
    H = login(); total_sent = 0
    for i, s in enumerate(wave['segments']):
        cid = s['campaignId']
        leads = [{'customerId': l['id'], 'assignedProvider': l['assignedProvider']}
                 for l in s['leads'] if l['assignedProvider'] in HEALTHY]
        qq = requests.post(base + '/api/v1/email-providers/queue-with-assignment',
                           headers=H, timeout=60, json={'campaignId': cid, 'leads': leads})
        jr = {}
        try: jr = qq.json()
        except: pass
        qc = jr.get('queuedCount', 0)
        print(f'[{ts()}] {s["seg"]:15} {s["service"]:22} | {qq.status_code} queued={qc} | {cid[:8]}')
        if qq.status_code not in (200, 201, 202):
            if 'Validation.Recipients' in qq.text: continue
            print(f'   !!! {qq.text[:200]}'); break
        if i == 0:
            sent = 0
            for t in range(42):
                time.sleep(10)
                d = requests.get(base + '/api/v1/email-campaigns/campaigns/' + cid, headers=H, timeout=20).json()
                sent = d.get('sentCount', 0)
                print(f'   +{(t+1)*10}s sent={sent}')
                if sent > 0: break
            if sent == 0: print(f'[{ts()}] !!! BREAKER'); return
            print(f'[{ts()}] breaker OK\n')
        if i < len(wave['segments']) - 1: time.sleep(15)
        total_sent += qc

    time.sleep(20)
    ph = {p['provider']: p for p in requests.get(base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    print(f'\n[{ts()}] === W11 DONE: {total_sent} queued ===')
    print({n: f"{ph.get(n,{}).get('sentToday',0)}/100" for n in HEALTHY})
    for s in wave['segments']:
        print(f'  {s["seg"]:15} {s["service"]:22} {len(s["leads"])} → {s["campaignId"][:8]}')


if __name__ == '__main__':
    run()
