# -*- coding: utf-8 -*-
"""
Wave 8 — leads pós-scraper. Novos combos nunca usados hoje:
  academia×Software, medico×Landing, imobiliaria×Software,
  salao×Landing, odontologia×Landing, farmacia×Apps.

Segue o padrão build/send dos W5-W7.
Múltiplos search_terms por nicho (API busca Name/Email/CompanyName, não Tags).
"""
import os, sys, re, json, time, datetime, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
from fire_today_20 import (
    DIR, base, HEALTHY, PER_PROVIDER,
    ts, load_env, login, qp, gen_image, build_html, email_ok,
    fetch_leads, assign_providers, ensure_breaker_closed,
    FREE_WEBMAIL, FOREIGN_TLD
)

WAVE_FILE = DIR + 'previews/wave-2026-06-20-w8.json'

# Múltiplos termos de busca por nicho — API não indexa Tags, então
# usamos vários termos para pegar nomes extraídos pelo scraper.
# Cada entrada é um passe de busca; dedup por email/empresa/domínio
# evita duplicatas entre passes e com waves anteriores.
SEGMENTS = [
    dict(
        seg='academia', search_terms=['academia', 'fitness', 'crossfit', 'gym'],
        service='Software de Gestão',
        hook='Um software de gestão pode transformar sua academia em uma máquina de retenção de alunos.',
        subject='Um software de gestão para a {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo softwares. Academias que usam software de gestão aumentam retenção de alunos, controlam inadimplência e automatizam cobranças — tudo em um painel simples, sem planilhas.',
        bullets=['Controle de matrículas, planos e renovações',
                 'Cobrança automática e controle de inadimplência',
                 'Agendamento de aulas e gerenciamento de instrutores',
                 'Aplicativo para o aluno acompanhar treinos e pagamentos'],
        wa='Ola! Quero um diagnostico rapido de um software de gestao para minha academia',
        img1='A gym manager using a modern software dashboard on a laptop at the reception desk, Brazilian fitness academy background, realistic editorial photo',
        img2='A Brazilian fitness academy with members training and modern management screens on the wall, realistic editorial photo',
    ),
    dict(
        seg='medico', search_terms=['medico', 'clinica', 'saude', 'consultorio', 'doutor'],
        service='Landing Page',
        hook='Uma landing page pode fazer pacientes encontrarem você no Google antes do consultório da esquina.',
        subject='Uma landing page de captação de pacientes para o Dr. {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Pacientes pesquisam médicos e especialistas no Google antes de ligar — uma página direta com especialidade, convênios e WhatsApp para agendamento gera consultas novas todo mês.',
        bullets=['Apresentação clara da especialidade e CRM médico',
                 'Formulário de agendamento com WhatsApp direto',
                 'Convênios e planos aceitos em destaque',
                 'Aparece no Google para quem busca sua especialidade na cidade'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para meu consultorio medico',
        img1='A Brazilian doctor reviewing a professional landing page on a tablet in a clean modern clinic, realistic editorial photo',
        img2='A welcoming Brazilian medical office reception with natural light and professional staff, realistic editorial photo',
    ),
    dict(
        seg='imobiliaria', search_terms=['imobiliaria', 'imoveis', 'corretor', 'imobil'],
        service='Software Imobiliário',
        hook='Um software imobiliário pode automatizar sua carteira de imóveis e gerar leads qualificados 24h.',
        subject='Um software imobiliário para a {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo softwares. Imobiliárias com software integrado publicam imóveis automaticamente nos portais, gerenciam visitas e recebem leads pelo site — sem perder oportunidade por demora no atendimento.',
        bullets=['Publicação automática nos portais ZAP, OLX, Viva Real',
                 'CRM de compradores e locatários com histórico completo',
                 'Geração de contratos e documentos digitalmente',
                 'Site próprio com busca de imóveis e formulário de contato'],
        wa='Ola! Quero um diagnostico rapido de um software imobiliario para minha imobiliaria',
        img1='A Brazilian real estate agent using a property management software on a laptop in a modern agency office, realistic editorial photo',
        img2='A modern Brazilian real estate office with property listings on large screens and professional agents, realistic editorial photo',
    ),
    dict(
        seg='salao', search_terms=['salao', 'beleza', 'hair', 'cabeleireiro', 'nail', 'studio'],
        service='Landing Page',
        hook='Uma landing page pode fazer clientes encontrarem seu salão no Google antes do concorrente.',
        subject='Uma landing page para o {{empresa}} que atrai clientes novos todo mês?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Clientes buscam salões de beleza no Google e no Instagram — uma página rápida com seus serviços, fotos dos resultados e agendamento pelo WhatsApp faz a diferença na captação.',
        bullets=['Galeria de antes/depois dos serviços',
                 'Lista de serviços e preços em destaque',
                 'Agendamento direto pelo WhatsApp com um clique',
                 'Aparece no Google para quem busca salão na sua cidade'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para meu salao de beleza',
        img1='A Brazilian hair salon stylist working on a client in a modern and well-lit beauty salon, realistic editorial photo',
        img2='A beautiful modern Brazilian beauty salon interior with professional styling stations and happy clients, realistic editorial photo',
    ),
    dict(
        seg='odontologia', search_terms=['odonto', 'dental', 'dentista', 'odontologia'],
        service='Landing Page',
        hook='Uma landing page pode fazer pacientes encontrarem sua clínica odontológica no Google antes da concorrência.',
        subject='Uma landing page de captação de pacientes para a {{empresa}}?',
        intro='Olá! Sou o Alexandre Queiroz e crio landing pages. Quem precisa de dentista pesquisa no Google — uma landing page com seus procedimentos, depoimentos e WhatsApp para agendamento rápido gera consultas novas sem depender só de indicação.',
        bullets=['Procedimentos e especialidades em destaque',
                 'Depoimentos de pacientes e casos de sucesso',
                 'Agendamento pelo WhatsApp ou formulário',
                 'Aparece no Google para quem busca dentista na sua cidade'],
        wa='Ola! Quero um diagnostico rapido de uma landing page para minha clinica odontologica',
        img1='A Brazilian dentist consulting with a patient in a modern dental clinic with professional equipment, realistic editorial photo',
        img2='A clean modern Brazilian dental clinic reception with friendly staff and bright welcoming environment, realistic editorial photo',
    ),
    dict(
        seg='farmacia', search_terms=['farmacia', 'drogaria', 'manipulacao', 'pharma'],
        service='App Mobile',
        hook='E se seus clientes pedissem medicamentos e agendassem a manipulação pelo app da sua farmácia?',
        subject='Um app mobile para a {{empresa}} fidelizar clientes e aumentar pedidos?',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Farmácias com app próprio recebem pedidos de manipulação, avisam quando o produto está pronto e enviam promoções para clientes fiéis — sem depender de marketplaces.',
        bullets=['Pedidos de manipulação e consulta de estoque pelo app',
                 'Notificação de produto pronto e prazo de entrega',
                 'Programa de fidelidade e promoções para clientes',
                 'Receituário digital e histórico de compras'],
        wa='Ola! Quero um diagnostico rapido de um app mobile para minha farmacia de manipulacao',
        img1='A Brazilian pharmacist showing a pharmacy app to a customer at a modern pharmacy counter, realistic editorial photo',
        img2='A modern Brazilian compounding pharmacy interior with organized shelves and professional staff, realistic editorial photo',
    ),
]

MAX_PER_SEG = 5  # máx leads por segmento (slots restantes: 223/6 ≈ 37, mas cap conservador)


def fetch_leads_multi(search_terms, H, seen_emails, seen_domains, seen_company, want):
    """Tenta múltiplos termos de busca, retorna leads únicos até want."""
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
    print(f'\n[{ts()}] ===== WAVE 8 BUILD+SEND — LEADS PÓS-SCRAPER =====')
    os.makedirs(DIR + 'previews', exist_ok=True)

    seen_emails: set = set()
    seen_domains: set = set()
    seen_company: set = set()

    # fetch_leads já filtra emailSentCount>0 / lastEmailSentAt, então
    # leads das waves 1-7 já não aparecem. Mas mantemos seen_* para dedup inter-segmentos.

    wave = {'date': '2026-06-20', 'wave': 8, 'created': ts(), 'segments': []}

    # ---- BUILD: gerar imagens, criar campanhas ----
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
        camp_name = f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-06-20 w8'
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
    print(f'\n[{ts()}] Wave 8 BUILD: {total_built} leads em {len(wave["segments"])} segmentos')
    if total_built == 0:
        print(f'[{ts()}] ABORT: nenhum lead disponível'); return

    # ---- SEND: queue-with-assignment + sonda breaker ----
    print(f'\n[{ts()}] ===== WAVE 8 SEND =====')
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
        print(f'[{ts()}] {s["seg"]:15} {s["service"]:24} | QUEUE {qq.status_code} queued={qc} | cid {cid[:8]}')
        if qq.status_code not in (200, 201, 202):
            err = qq.text[:300]
            if 'Validation.Recipients' in err:
                print(f'   (já enfileirado — pulando)')
                continue
            print(f'   !!! QUEUE rejeitado: {err}'); break

        # Sonda breaker no primeiro segmento
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
    print(f'\n[{ts()}] === WAVE 8 COMPLETE: {total_sent} queued ===')
    print('Limites:', {n: f"{ph.get(n,{}).get('sentToday',0)}/{ph.get(n,{}).get('dailyLimit',100)}"
                       for n in HEALTHY})
    for s in wave['segments']:
        print(f'  {s["seg"]:15} {s["service"]:24} {len(s["leads"])} leads → {s["campaignId"][:8]}')


if __name__ == '__main__':
    run()
