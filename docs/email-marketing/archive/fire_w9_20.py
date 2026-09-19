# -*- coding: utf-8 -*-
"""
Wave 9 — leads pós-scraper, 2ª rodada. Combos novos:
  academia×Apps, medico×Software, imobiliaria×Apps,
  salao×Software, odontologia×Software, farmacia×Landing.

Padrão idêntico ao W8.
"""
import os, sys, re, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, qp, gen_image, build_html, email_ok,
    fetch_leads, assign_providers, ensure_breaker_closed,
    FREE_WEBMAIL, FOREIGN_TLD
)

WAVE_FILE = DIR + 'previews/wave-2026-06-20-w9.json'

SEGMENTS = [
    dict(
        seg='academia', search_terms=['academia', 'fitness', 'crossfit', 'gym'],
        service='App Mobile',
        hook='E se seus alunos gerenciassem treinos, planos e pagamentos pelo app da sua academia?',
        subject='Um app mobile para a {{empresa}} fidelizar alunos e vender mais planos?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Academias com app próprio aumentam engajamento dos alunos, reduzem cancelamentos e geram receita extra com venda de planos e produtos diretamente no celular.',
        bullets=['Treinos personalizados e histórico de evolução no app',
                 'Renovação e pagamento de plano pelo celular',
                 'Notificações de aulas, promoções e novidades',
                 'Check-in digital e controle de frequência'],
        wa='Ola! Quero um diagnostico rapido de um app mobile para minha academia',
        img1='A gym member checking their workout plan on a fitness app on a smartphone at a Brazilian academy, realistic editorial photo',
        img2='A Brazilian gym instructor reviewing member progress on a tablet in a modern fitness academy, realistic editorial photo',
    ),
    dict(
        seg='medico', search_terms=['medico', 'clinica', 'saude', 'consultorio', 'doutor'],
        service='Software de Gestão',
        hook='Um software de gestão pode eliminar planilhas e automatizar o agendamento do seu consultório.',
        subject='Um software de gestão clínica para o Dr. {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo softwares. Consultórios e clínicas com software de gestão eliminam planilhas, automatizam lembretes de consulta por WhatsApp e têm prontuário eletrônico — menos trabalho administrativo, mais tempo para pacientes.',
        bullets=['Prontuário eletrônico e histórico do paciente centralizado',
                 'Agendamento online e lembretes automáticos por WhatsApp',
                 'Controle financeiro de consultas e convênios',
                 'Prescrições digitais e laudos em PDF'],
        wa='Ola! Quero um diagnostico rapido de um software de gestao clinica para meu consultorio',
        img1='A Brazilian doctor using a clinic management software on a computer in a clean medical office, realistic editorial photo',
        img2='A modern Brazilian clinic with digital scheduling screens and efficient reception staff, realistic editorial photo',
    ),
    dict(
        seg='imobiliaria', search_terms=['imobiliaria', 'imoveis', 'corretor', 'imobil'],
        service='App Mobile',
        hook='E se compradores e locatários buscassem imóveis diretamente pelo app da sua imobiliária?',
        subject='Um app mobile de busca de imóveis para a {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Imobiliárias com app próprio oferecem busca de imóveis por filtro, agendamento de visitas e notificação de novas oportunidades — uma experiência premium que fideliza o cliente e diferencia da concorrência.',
        bullets=['Busca de imóveis por filtros: tipo, bairro, valor, quartos',
                 'Agendamento de visitas diretamente pelo app',
                 'Notificações de novos imóveis que combinam com a busca',
                 'Chat direto com o corretor responsável'],
        wa='Ola! Quero um diagnostico rapido de um app de busca de imoveis para minha imobiliaria',
        img1='A real estate agent showing property listings on a mobile app to clients in a modern Brazilian real estate office, realistic editorial photo',
        img2='A couple browsing real estate mobile app on a smartphone while visiting a Brazilian property, realistic editorial photo',
    ),
    dict(
        seg='salao', search_terms=['salao', 'beleza', 'hair', 'cabeleireiro', 'nail', 'studio'],
        service='Software de Gestão',
        hook='Um software de gestão pode automatizar o agendamento e fidelizar clientes do seu salão.',
        subject='Um software de gestão para o {{empresa}} crescer sem virar escravo da agenda?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo softwares. Salões de beleza com software de gestão automatizam agendamentos online, enviam lembretes por WhatsApp, controlam caixa e geram relatórios de desempenho — sem depender de caderno ou planilha.',
        bullets=['Agendamento online 24h pelos clientes sem ligar',
                 'Lembretes automáticos por WhatsApp para reduzir faltas',
                 'Controle de caixa, comissões e estoque de produtos',
                 'Histórico do cliente e serviços favoritos'],
        wa='Ola! Quero um diagnostico rapido de um software de gestao para meu salao de beleza',
        img1='A hair salon owner using management software on a tablet to manage appointments at the reception desk, realistic editorial photo',
        img2='A busy Brazilian beauty salon with multiple styling stations and professional stylists working with clients, realistic editorial photo',
    ),
    dict(
        seg='odontologia', search_terms=['odonto', 'dental', 'dentista', 'odontologia'],
        service='Software de Gestão',
        hook='Um software odontológico pode eliminar prontuários em papel e automatizar lembretes de consulta.',
        subject='Um software de gestão odontológica para a {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo softwares. Clínicas odontológicas com software próprio têm prontuário digital, controle de tratamentos em andamento, envio de lembretes de retorno por WhatsApp e relatórios financeiros — tudo em um só lugar.',
        bullets=['Prontuário odontológico digital com odontograma',
                 'Lembretes automáticos de retorno e manutenção',
                 'Controle de convênios, orçamentos e pagamentos',
                 'Agenda online com confirmação automática'],
        wa='Ola! Quero um diagnostico rapido de um software de gestao para minha clinica odontologica',
        img1='A Brazilian dentist using dental management software on a computer beside a patient chair in a modern dental clinic, realistic editorial photo',
        img2='A clean Brazilian dental office with digital odontogram on screen and professional equipment, realistic editorial photo',
    ),
    dict(
        seg='farmacia', search_terms=['farmacia', 'drogaria', 'manipulacao', 'pharma'],
        service='Landing Page',
        hook='Uma landing page pode fazer clientes encontrarem sua farmácia de manipulação no Google antes da concorrência.',
        subject='Uma landing page para a {{empresa}} captar clientes pelo Google?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Quem busca farmácia de manipulação no Google quer especialidade, confiança e facilidade de contato — uma landing page com seus diferenciais, especialidades e WhatsApp para orçamento gera clientes novos todo mês.',
        bullets=['Apresentação das especialidades e fórmulas exclusivas',
                 'Aparece no Google para quem busca farmácia de manipulação',
                 'Formulário de orçamento e WhatsApp para pedidos',
                 'Depoimentos de clientes e certificações em destaque'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para minha farmacia de manipulacao',
        img1='A Brazilian compounding pharmacist preparing a custom formula at a modern pharmacy laboratory, realistic editorial photo',
        img2='A professional Brazilian compounding pharmacy interior with certificates on the wall and organized workstation, realistic editorial photo',
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
    print(f'\n[{ts()}] ===== WAVE 9 BUILD+SEND — LEADS PÓS-SCRAPER (2ª RODADA) =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_emails: set = set()
    seen_domains: set = set()
    seen_company: set = set()

    wave = {'date': '2026-06-20', 'wave': 9, 'created': ts(), 'segments': []}

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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-20 w9'
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
    print(f'\n[{ts()}] Wave 9 BUILD: {total_built} leads em {len(wave["segments"])} segmentos')
    if total_built == 0:
        print(f'[{ts()}] INVENTÁRIO ESGOTADO — sem leads disponíveis'); return

    # ---- SEND ----
    print(f'\n[{ts()}] ===== WAVE 9 SEND =====')
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
    print(f'\n[{ts()}] === WAVE 9 COMPLETE: {total_sent} queued ===')
    print('Limites:', {n: f"{ph.get(n,{}).get('sentToday',0)}/{ph.get(n,{}).get('dailyLimit',100)}"
                       for n in HEALTHY})
    for s in wave['segments']:
        print(f'  {s["seg"]:15} {s["service"]:26} {len(s["leads"])} leads → {s["campaignId"][:8]}')


if __name__ == '__main__':
    run()
