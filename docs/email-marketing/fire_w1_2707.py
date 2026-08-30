# -*- coding: utf-8 -*-
"""Wave 1 — retomada 2026-07-27 (segunda 09:00 BRT) — template v2.
Segmentos novos (estoque nunca-contactado): logistica, arquitetura, estudio (yoga/pilates), medico.
v2: assunto de clique, corpo estruturado, IMAGEM PRINCIPAL NO MEIO, 1 imagem só,
linha de credibilidade, CTA WhatsApp. Fila é reagendada p/ 27/07 12:00 UTC (09:00 BRT)
logo após cada queue (worker só pega ScheduledAt <= now)."""
import os, sys, json, datetime, urllib.parse, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
from fire_today_20 import (DIR, base, HEALTHY, PER_PROVIDER, ts, load_env, login,
                           gen_image, fetch_leads, assign_providers, ensure_breaker_closed)
import db as crmdb

WAVE_FILE = DIR + 'previews/wave-2026-07-27-w1.json'
SEND_AT_SQL = '2026-07-27 12:00:00'  # UTC = 09:00 BRT segunda

def qp(s): return urllib.parse.quote(s)

def build_html_v2(hook, subhead, img_main, intro, bullets, wa):
    """v2: header dark + hook + subhead + intro -> IMAGEM PRINCIPAL NO MEIO -> entregas -> credibilidade -> CTA -> footer."""
    bl = "".join(
        '<tr><td style="padding:0 0 12px 0;">'
        '<table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr>'
        '<td valign="top" style="padding:2px 10px 0 0;"><span style="display:inline-block;width:20px;height:20px;border-radius:10px;background-color:#e0f2fe;color:#0369a1;font-family:Arial,Helvetica,sans-serif;font-size:13px;font-weight:bold;text-align:center;line-height:20px;">&#10003;</span></td>'
        '<td style="font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:24px;color:#0f172a;">' + b + '</td>'
        '</tr></table></td></tr>' for b in bullets)
    return ('<!DOCTYPE html><html lang="pt-BR"><head><meta charset="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1.0"/></head>'
    '<body style="margin:0;padding:0;background-color:#f1f5f9;">'
    '<div style="display:none;max-height:0;overflow:hidden;mso-hide:all;">' + subhead + '</div>'
    '<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#f1f5f9;"><tr><td align="center" style="padding:28px 12px;">'
    '<table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:600px;max-width:600px;background-color:#ffffff;border-radius:14px;overflow:hidden;">'
    # header
    '<tr><td align="left" style="background-color:#0f172a;padding:20px 32px;"><img src="https://www.alexandrequeiroz.com.br/images/logo.png" alt="Alexandre Queiroz" width="170" style="display:block;border:0;height:auto;width:170px;"/></td></tr>'
    '<tr><td style="height:4px;background:#57b3df;line-height:4px;font-size:4px;">&nbsp;</td></tr>'
    # hook + subhead
    '<tr><td style="padding:36px 32px 8px 32px;"><h1 style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:27px;line-height:35px;color:#0f172a;font-weight:bold;">' + hook + '</h1></td></tr>'
    '<tr><td style="padding:0 32px 18px 32px;"><p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:24px;color:#64748b;">' + subhead + '</p></td></tr>'
    # intro
    '<tr><td style="padding:0 32px 22px 32px;"><p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:26px;color:#334155;">' + intro + '</p></td></tr>'
    # IMAGEM PRINCIPAL — NO MEIO DO EMAIL
    '<tr><td style="padding:0 32px 26px 32px;font-size:0;line-height:0;"><img src="' + img_main + '" alt="" width="536" style="display:block;width:100%;max-width:536px;height:auto;border:0;border-radius:12px;"/></td></tr>'
    # entregas
    '<tr><td style="padding:0 32px 4px 32px;"><p style="margin:0 0 14px 0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:24px;color:#0f172a;font-weight:bold;">Para a {{empresa}}, eu entrego:</p>'
    '<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">' + bl + '</table></td></tr>'
    # credibilidade
    '<tr><td style="padding:18px 32px 0 32px;"><table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"><tr><td style="background-color:#f8fafc;border-left:4px solid #57b3df;border-radius:8px;padding:14px 18px;font-family:Arial,Helvetica,sans-serif;font-size:14px;line-height:22px;color:#475569;">Engenheiro de software s&ecirc;nior &mdash; <strong>15+ anos</strong> construindo sistemas para multinacionais e neg&oacute;cios locais do ES.</td></tr></table></td></tr>'
    # CTA
    '<tr><td align="center" style="padding:28px 32px 8px 32px;"><table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr><td align="center" bgcolor="#25D366" style="border-radius:10px;"><a href="https://wa.me/5527999840101?text=' + qp(wa) + '&amp;utm_source=email&amp;utm_medium=campanha&amp;utm_campaign=aq_wave_2707" target="_blank" style="display:inline-block;padding:16px 34px;font-family:Arial,Helvetica,sans-serif;font-size:17px;line-height:21px;font-weight:bold;color:#ffffff;text-decoration:none;border-radius:10px;">Quero um diagn&oacute;stico gratuito no WhatsApp &#8250;</a></td></tr></table>'
    '<p style="margin:10px 0 0 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#64748b;">Sem compromisso &middot; resposta no mesmo dia &uacute;til.</p></td></tr>'
    # assinatura
    '<tr><td align="center" style="padding:26px 32px 24px 32px;"><p style="margin:0;font-family:Georgia,serif;font-size:18px;line-height:26px;color:#0f172a;font-style:italic;">&ldquo;Tecnologia que faz o seu neg&oacute;cio vender mais.&rdquo;</p></td></tr>'
    # footer
    '<tr><td style="background-color:#0f172a;padding:24px 32px;" align="center"><img src="https://www.alexandrequeiroz.com.br/images/logo.png" alt="AQ" width="150" style="display:block;border:0;height:auto;width:150px;margin:0 auto 12px auto;"/>'
    '<p style="margin:0 0 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#cbd5e1;">Sites &middot; Aplicativos &middot; Sistemas sob demanda &middot; Landing pages</p>'
    '<p style="margin:0 0 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#cbd5e1;">WhatsApp (27) 99984-0101 &middot; contato@alexandrequeiroz.com.br</p>'
    '<p style="margin:0 0 10px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#57b3df;">www.alexandrequeiroz.com.br</p>'
    '<p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:11px;line-height:17px;color:#64748b;">Vit&oacute;ria, ES &middot; <a href="{{unsubscribe_url}}" target="_blank" style="color:#64748b;text-decoration:underline;">descadastrar</a></p></td></tr>'
    '</table></td></tr></table></body></html>')

SEGMENTS = [
    dict(seg='logistica', search_terms=['logistica', 'transporte', 'transportes', 'armazem'],
        service='Sistema de Gestao',
        subject='{{empresa}}: quantos fretes se perdem na planilha todo mês?',
        hook='Fretes, rotas e custos num painel só — sem planilha, sem retrabalho.',
        subhead='Sistema sob medida para a operação logística, do jeito que ela funciona hoje.',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Operação logística controlada em planilha perde frete, atrasa cobrança e esconde custo — um sistema feito para o seu fluxo devolve o controle no mesmo dia.',
        bullets=['Painel de fretes e cargas em tempo real',
                 'Custos por rota, veículo e cliente sem digitação dupla',
                 'Portal do cliente para rastrear entregas sozinho',
                 'Alertas de atraso e cobrança automática'],
        wa='Ola! Quero um diagnostico rapido de um sistema para minha operacao logistica',
        img='A Brazilian logistics manager monitoring a freight management dashboard on a large screen in a modern warehouse office, trucks visible through the window, realistic editorial photo'),
    dict(seg='arquitetura', search_terms=['arquitetura', 'arquiteto', 'arquitetos'],
        service='Site Portfolio',
        subject='O portfólio da {{empresa}} aparece no Google — ou só no Instagram?',
        hook='Quem procura arquiteto no Google precisa encontrar o seu portfólio.',
        subhead='Site profissional que transforma projeto entregue em cliente novo.',
        intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Instagram mostra o projeto para quem já te segue — o site coloca seu portfólio na frente de quem está pesquisando arquiteto agora, com a sofisticação que o seu trabalho pede.',
        bullets=['Portfólio em alta resolução que valoriza cada projeto',
                 'Aparecer no Google de quem busca arquiteto na sua cidade',
                 'Página de serviços que filtra o cliente certo',
                 'Contato direto no WhatsApp a partir de qualquer projeto'],
        wa='Ola! Quero um diagnostico rapido de um site portfolio para meu escritorio de arquitetura',
        img='A Brazilian architect presenting an elegant portfolio website on a large monitor in a modern architecture studio with scale models and blueprints, realistic editorial photo'),
    dict(seg='estudio', search_terms=['yoga', 'pilates', 'estudio'],
        service='App de Agendamento',
        subject='Seus alunos agendando sozinhos — sem WhatsApp o dia inteiro',
        hook='Um app com a sua marca: aula marcada, lembrete enviado, falta reduzida.',
        subhead='Agendamento, pacotes e fidelização direto no celular do aluno.',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Estúdio que agenda por WhatsApp perde hora respondendo e perde aluno quando esquece — com app próprio, o aluno marca, remarca e recebe lembrete sozinho.',
        bullets=['Agendamento de aulas 24h pelo app, sem mensagens',
                 'Lembretes automáticos que reduzem falta',
                 'Controle de pacotes, créditos e vencimentos',
                 'Publicado na App Store e Google Play com a sua marca'],
        wa='Ola! Quero um diagnostico rapido de um app de agendamento para meu estudio',
        img='A Brazilian yoga studio owner showing a class booking app on a smartphone to a student at a bright modern studio reception, mats and plants in background, realistic editorial photo'),
    dict(seg='medico', search_terms=['medico', 'consultorio', 'cardiologia', 'dermatologia'],
        service='Site com Agendamento',
        subject='Pacientes agendando na {{empresa}} — até fora do horário da recepção',
        hook='Consultório que agenda online atende quem procura médico à noite.',
        subhead='Site profissional com agendamento que trabalha quando a recepção não está.',
        intro='Olá! Sou o Alexandre Queiroz e crio sites para a área da saúde. Boa parte dos pacientes procura médico fora do horário comercial — site com agendamento online captura essa consulta que hoje vai para outro consultório.',
        bullets=['Agendamento online integrado à agenda do consultório',
                 'Aparecer no Google de quem busca a sua especialidade',
                 'Página que transmite confiança: equipe, estrutura, convênios',
                 'Lembretes que reduzem falta de paciente'],
        wa='Ola! Quero um diagnostico rapido de um site com agendamento para meu consultorio',
        img='A Brazilian doctor in a modern medical office reviewing an online appointment website on a laptop, clean bright clinic interior, realistic editorial photo'),
]

MAX_PER_SEG = PER_PROVIDER * len(HEALTHY)  # 5 x 4 = 20


def fetch_leads_multi(search_terms, H, seen_e, seen_d, seen_c, want):
    all_leads = []
    for term in search_terms:
        if len(all_leads) >= want: break
        leads, _ = fetch_leads({'search': term}, H, seen_e, seen_d, seen_c, want - len(all_leads))
        if leads:
            print(f'   search="{term}" -> {len(leads)} leads')
            all_leads.extend(leads)
    return all_leads[:want]


def reschedule(campaign_id):
    """Reagenda itens Queued da campanha p/ segunda 09:00 BRT (worker so pega ScheduledAt<=now)."""
    cx = crmdb.connect(autocommit=False); cur = cx.cursor()
    cur.execute("UPDATE email_queue_items SET scheduled_at=?, updated_at=SYSUTCDATETIME(), updated_by='fire_w1_2707' "
                "WHERE campaign_id=? AND status=0  # EmailQueueStatus.Queued = 0", SEND_AT_SQL, campaign_id)
    n = cur.rowcount; cx.commit(); cx.close()
    return n


def run():
    load_env(); H = login()
    print(f'\n[{ts()}] ===== WAVE 1 -- 27/07 (agendada seg 09:00 BRT) -- template v2 =====')
    os.makedirs(DIR + 'previews', exist_ok=True)
    seen_e, seen_d, seen_c = set(), set(), set()
    wave = {'date': '2026-07-27', 'wave': 1, 'created': ts(), 'template': 'v2', 'segments': []}

    for cfg in SEGMENTS:
        print(f'\n[{ts()}] --- {cfg["seg"].upper()} x {cfg["service"]} ---')
        leads = fetch_leads_multi(cfg['search_terms'], H, seen_e, seen_d, seen_c, MAX_PER_SEG)
        if not leads:
            print('   0 leads disponiveis -- pulando'); continue
        leads = assign_providers(leads)
        print(f'[{ts()}]   {len(leads)} leads selecionados')

        img = gen_image(cfg['img'], H, keyword=cfg['seg'])
        if not img:
            print('   !!! sem imagem — pulando segmento (v2 exige imagem principal)'); continue
        html = build_html_v2(cfg['hook'], cfg['subhead'], img, cfg['intro'], cfg['bullets'], cfg['wa'])
        open(DIR + f'templates/wave2707-{cfg["seg"]}.html', 'w', encoding='utf-8').write(html)

        H2 = login()
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H2, timeout=30,
                           json={'name': f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-07-27 w1',
                                 'subject': cfg['subject'],
                                 'fromName': 'Alexandre Queiroz',
                                 'fromEmail': 'contato@alexandrequeiroz.com.br',
                                 'bodyHtml': html})
        if cr.status_code not in (200, 201):
            print(f'   !!! CRIACAO FALHOU {cr.status_code}: {cr.text[:250]}'); continue
        cid = cr.json().get('id') or cr.json().get('campaignId')
        for ld in leads: ld['campaignId'] = cid
        wave['segments'].append({'seg': cfg['seg'], 'service': cfg['service'], 'campaignId': cid, 'leads': leads})
        print(f'[{ts()}]   cid={cid[:8]} | template salvo')

    json.dump(wave, open(WAVE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    total = sum(len(s['leads']) for s in wave['segments'])
    print(f'\n[{ts()}] BUILD: {total} leads em {len(wave["segments"])} segmentos')
    if not total: print('INVENTARIO ESGOTADO'); return

    print(f'\n[{ts()}] ===== QUEUE (+ reagenda p/ 27/07 12:00 UTC) =====')
    if not ensure_breaker_closed(H): print('[ABORT] circuit breaker aberto'); return
    H = login()
    for seg in wave['segments']:
        cid = seg['campaignId']
        payload = [{'customerId': ld['id'], 'assignedProvider': ld['assignedProvider']} for ld in seg['leads']]
        r = requests.post(base + '/api/v1/email-providers/queue-with-assignment', headers=H, timeout=60,
                          json={'campaignId': cid, 'leads': payload})
        jr = r.json() if r.status_code in (200, 201, 202) else {}
        moved = reschedule(cid)
        print(f'[{ts()}] {seg["seg"]:12} {seg["service"]:20} | QUEUE {r.status_code} queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | reagendados={moved} | cid {cid}')
    print(f'\n[{ts()}] Wave 1 27/07 -- PRONTA (dispara segunda 09:00 BRT)')


if __name__ == '__main__':
    run()
