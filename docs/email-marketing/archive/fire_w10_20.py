# -*- coding: utf-8 -*-
"""
Wave 10 — nichos das W1-W7 com combos de serviço ainda não usados hoje:
  clinica×Apps, contabilidade×Software, advocacia×Landing,
  hotel×Software, escola×Apps, loja×Sites.
fetch_leads filtra emailSentCount>0 automaticamente — só chega lead nunca-contatado.
"""
import os, sys, re, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, qp, gen_image, build_html, email_ok,
    fetch_leads, assign_providers, ensure_breaker_closed,
    FREE_WEBMAIL, FOREIGN_TLD
)

WAVE_FILE = DIR + 'previews/wave-2026-06-20-w10.json'

SEGMENTS = [
    dict(
        seg='clinica', search_terms=['clinica', 'medica', 'saude'],
        service='App Mobile',
        hook='E se os pacientes da sua clínica pudessem agendar consultas e ver resultados de exames pelo celular?',
        subject='Um app de pacientes para a {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Clínicas com app próprio oferecem agendamento online, notificação de resultados de exames e telemedicina — reduzem ligações na recepção e elevam a experiência do paciente.',
        bullets=['Agendamento de consultas e exames pelo app',
                 'Notificações de resultados e lembretes de retorno',
                 'Telemedicina e consulta por vídeo',
                 'Histórico de consultas e prescrições do paciente'],
        wa='Ola! Quero um diagnostico rapido de um app de pacientes para minha clinica',
        img1='A patient using a clinic mobile app to schedule an appointment on a smartphone at a modern Brazilian clinic waiting room, realistic editorial photo',
        img2='A Brazilian clinic doctor showing the patient app features on a tablet during a consultation, realistic editorial photo',
    ),
    dict(
        seg='contabilidade', search_terms=['contabilidade', 'contador', 'contabil'],
        service='Software de Gestão',
        hook='Um software de gestão pode automatizar a contabilidade de dezenas de clientes ao mesmo tempo.',
        subject='Um software de gestão contábil para a {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo softwares. Escritórios de contabilidade com software próprio automatizam obrigações fiscais, integram com a Receita Federal e entregam relatórios para clientes de forma automática — menos trabalho manual, mais clientes atendidos.',
        bullets=['Integração automática com SPED, eSocial e NFe',
                 'Dashboard para clientes acompanhar resultado financeiro',
                 'Controle de prazos e obrigações fiscais por cliente',
                 'Emissão automática de guias e relatórios mensais'],
        wa='Ola! Quero um diagnostico rapido de um software de gestao contabil para meu escritorio',
        img1='A Brazilian accountant using modern accounting software on dual monitors in a professional office, realistic editorial photo',
        img2='A professional Brazilian accounting office with organized workstations and team reviewing financial reports, realistic editorial photo',
    ),
    dict(
        seg='advocacia', search_terms=['advocacia', 'advogado', 'juridico', 'adv'],
        service='Landing Page',
        hook='Uma landing page pode fazer clientes encontrarem o seu escritório de advocacia no Google 24 horas por dia.',
        subject='Uma landing page de captação de clientes para o escritório {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Quem precisa de advogado pesquisa no Google — uma landing page com suas áreas de atuação, casos de sucesso e WhatsApp para consulta gratuita converte visitantes em clientes todos os dias.',
        bullets=['Áreas de atuação e especialidades em destaque',
                 'Aparece no Google para quem busca advogado na cidade',
                 'Formulário de consulta e WhatsApp para atendimento rápido',
                 'Depoimentos de clientes e credenciais da OAB'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para meu escritorio de advocacia',
        img1='A Brazilian lawyer reviewing a professional website on a laptop in a modern law office with bookshelves, realistic editorial photo',
        img2='A professional Brazilian law office with organized desk and certificates on the wall, realistic editorial photo',
    ),
    dict(
        seg='hotel', search_terms=['hotel', 'pousada', 'hospedagem', 'resort'],
        service='Software de Gestão',
        hook='Um software hoteleiro pode automatizar reservas, check-in e cobranças do seu hotel.',
        subject='Um software de gestão hoteleira para o {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo softwares. Hotéis com software de gestão integrado controlam reservas em tempo real, emitem nota fiscal automática e gerenciam housekeeping — sem planilhas, sem overbooking, com relatórios diários automáticos.',
        bullets=['Gestão de reservas integrada com Booking, Airbnb e Expedia',
                 'Check-in e check-out digital sem fila na recepção',
                 'Controle de limpeza e manutenção dos quartos',
                 'Relatórios financeiros e taxa de ocupação em tempo real'],
        wa='Ola! Quero um diagnostico rapido de um software de gestao hoteleira para meu hotel',
        img1='A hotel manager using hospitality management software on a computer at the front desk of a modern Brazilian hotel, realistic editorial photo',
        img2='A well-organized Brazilian hotel front desk with management screens and professional staff, realistic editorial photo',
    ),
    dict(
        seg='escola', search_terms=['escola', 'colegio', 'educacao', 'ensino'],
        service='App Mobile',
        hook='E se os pais acompanhassem notas, frequência e comunicados da escola pelo celular?',
        subject='Um app escolar para os pais da {{empresa}} ficarem sempre informados?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Escolas com app próprio enviam comunicados, boletins e frequência para os pais em tempo real — reduzem circulares impressas, melhoram a comunicação e elevam a percepção de qualidade da escola.',
        bullets=['Boletim e frequência dos alunos em tempo real pelo app',
                 'Comunicados, circulares e eventos push notification',
                 'Chat entre professores e responsáveis',
                 'Cardápio da cantina e calendário escolar'],
        wa='Ola! Quero um diagnostico rapido de um app escolar para minha escola',
        img1='A parent checking their child school app on a smartphone showing grades and attendance, realistic editorial photo',
        img2='A Brazilian school administrator showing the school mobile app to parents at a modern school, realistic editorial photo',
    ),
    dict(
        seg='loja', search_terms=['loja', 'comercio', 'varejo', 'vendas'],
        service='Sites',
        hook='Sua loja aparece no Google quando alguém na sua cidade pesquisa o que você vende?',
        subject='Um site profissional para a {{empresa}} vender mais pela internet?',
        intro='Olá! Sou o Alexandre Queiroz e crio sites. Lojas com site próprio aparecem no Google local, recebem pedidos pelo WhatsApp e exibem catálogo de produtos 24h — sem pagar comissão para marketplace, com a sua marca em destaque.',
        bullets=['Catálogo de produtos com fotos e preços atualizados',
                 'Aparece no Google para quem busca o produto na cidade',
                 'Botão de pedido direto pelo WhatsApp',
                 'Rápido no celular e fácil de atualizar'],
        wa='Ola! Quero um diagnostico rapido de um site para minha loja',
        img1='A Brazilian retail store owner updating product catalog on a professional website on a laptop, realistic editorial photo',
        img2='A modern Brazilian retail store with organized product display and customers browsing, realistic editorial photo',
    ),
]

MAX_PER_SEG = 5


def fetch_leads_multi(search_terms, H, seen_emails, seen_domains, seen_company, want):
    all_leads = []
    for term in search_terms:
        if len(all_leads) >= want:
            break
        seg_cfg = {'search': term}
        leads, _ = fetch_leads(seg_cfg, H, seen_emails, seen_domains, seen_company, want - len(all_leads))
        if leads:
            print(f'   search="{term}" → {len(leads)} leads')
            all_leads.extend(leads)
    return all_leads[:want]


def run():
    load_env()
    H = login()
    print(f'\n[{ts()}] ===== WAVE 10 BUILD+SEND — COMBOS REMANESCENTES =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_emails: set = set()
    seen_domains: set = set()
    seen_company: set = set()

    wave = {'date': '2026-06-20', 'wave': 10, 'created': ts(), 'segments': []}

    # ---- BUILD ----
    for cfg in SEGMENTS:
        print(f'\n[{ts()}] --- {cfg["seg"].upper()} × {cfg["service"]} ---')
        leads = fetch_leads_multi(cfg['search_terms'], H, seen_emails, seen_domains, seen_company, MAX_PER_SEG)
        if not leads:
            print(f'   0 leads disponíveis — pulando')
            continue

        leads = assign_providers(leads)
        print(f'[{ts()}]   {len(leads)} leads selecionados')

        print(f'[{ts()}]   img1 gerando...')
        img1 = gen_image(cfg['img1'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img1={str(img1)[:60]}')
        print(f'[{ts()}]   img2 gerando...')
        img2 = gen_image(cfg['img2'], H, keyword=cfg['seg'])
        print(f'[{ts()}]   img2={str(img2)[:60]}')

        html = build_html(cfg['hook'], img1 or '', img2 or '', cfg['intro'], cfg['bullets'], cfg['wa'])

        H2 = login()
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-20 w10'
        future = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(minutes=5))
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H2,
                           json={'name': camp_name, 'subject': cfg['subject'],
                                 'fromName': 'Alexandre Queiroz',
                                 'fromEmail': 'contato@alexandrequeiroz.com.br',
                                 'bodyHtml': html,
                                 'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=30)
        if cr.status_code not in (200, 201):
            print(f'   !!! CRIAÇÃO FALHOU {cr.status_code}: {cr.text[:300]}')
            continue
        cid = cr.json().get('id') or cr.json().get('campaignId')
        sc = requests.post(base + '/api/v1/email-campaigns/campaigns/' + cid + '/schedule',
                           headers=H2, json={'scheduledAt': future.strftime('%Y-%m-%dT%H:%M:%SZ')}, timeout=20)
        print(f'[{ts()}]   cid={cid[:8]} schedule={sc.status_code}')

        for ld in leads:
            ld['campaignId'] = cid
        wave['segments'].append({
            'seg': cfg['seg'], 'service': cfg['service'],
            'campaignId': cid, 'leads': leads
        })

    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    total_built = sum(len(s['leads']) for s in wave['segments'])
    print(f'\n[{ts()}] Wave 10 BUILD: {total_built} leads em {len(wave["segments"])} segmentos')
    if total_built == 0:
        print(f'[{ts()}] INVENTÁRIO ESGOTADO — dia encerrado'); return

    # ---- SEND ----
    print(f'\n[{ts()}] ===== WAVE 10 SEND =====')
    if not ensure_breaker_closed(H):
        print('[ABORT] circuit breaker aberto'); return

    H = login()
    total_sent = 0
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
        print(f'[{ts()}] {s["seg"]:15} {s["service"]:26} | QUEUE {qq.status_code} queued={qc} | cid {cid[:8]}')
        if qq.status_code not in (200, 201, 202):
            err = qq.text[:300]
            if 'Validation.Recipients' in err:
                print(f'   (já enfileirado — pulando)')
                continue
            print(f'   !!! QUEUE rejeitado: {err}'); break

        if i == 0:
            print(f'[{ts()}] sonda breaker...')
            sent = 0
            for t in range(42):
                time.sleep(10)
                d = requests.get(base + '/api/v1/email-campaigns/campaigns/' + cid,
                                 headers=H, timeout=20).json()
                sent = d.get('sentCount', 0)
                print(f'   +{(t+1)*10}s sent={sent} del={d.get("deliveredCount",0)} fail={d.get("failedCount",0)}')
                if sent > 0: break
            if sent == 0:
                print(f'\n[{ts()}] !!! BREAKER ABERTO — 0 enviados em 420s'); return
            print(f'[{ts()}] breaker OK\n')

        if i < len(wave['segments']) - 1:
            time.sleep(15)
        total_sent += qc

    time.sleep(20)
    ph = {p['provider']: p for p in requests.get(
        base + '/api/v1/email-providers/health', headers=H, timeout=20).json()}
    print(f'\n[{ts()}] === WAVE 10 COMPLETE: {total_sent} queued ===')
    print('Limites:', {n: f"{ph.get(n,{}).get('sentToday',0)}/{ph.get(n,{}).get('dailyLimit',100)}"
                       for n in HEALTHY})
    for s in wave['segments']:
        print(f'  {s["seg"]:15} {s["service"]:26} {len(s["leads"])} leads → {s["campaignId"][:8]}')


if __name__ == '__main__':
    run()
