# -*- coding: utf-8 -*-
"""Waves 2 e 3 — 2026-07-27 (segunda), template v2, escalonadas:
w2 10:30 BRT (13:30 UTC): engenharia, advocacia, escola, marketing
w3 11:30 BRT (14:30 UTC): moveis, academia, otica, pousada
Dedupe: seen sets compartilhados + preload dos 43 leads da w1 (ainda nao-enviados)."""
import os, sys, json, requests
sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
from fire_today_20 import (DIR, base, HEALTHY, PER_PROVIDER, ts, load_env, login,
                           gen_image, fetch_leads, assign_providers, ensure_breaker_closed)
from fire_w1_2707 import build_html_v2
import db as crmdb

MAX_PER_SEG = PER_PROVIDER * len(HEALTHY)

WAVES = [
 dict(wave=2, send_api='2026-07-27T13:30:00Z', send_sql='2026-07-27 13:30:00',
      file=DIR+'previews/wave-2026-07-27-w2.json', segments=[
  dict(seg='engenharia', search_terms=['engenharia', 'engenheiro'], service='Sistema sob medida',
    subject='{{empresa}}: obra controlada em planilha ainda fecha a conta?',
    hook='Medições, custos e cronograma da obra num sistema só.',
    subhead='Software sob medida para engenharia — do orçamento à medição final.',
    intro='Olá! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Obra gerida em planilha espalha versão, atrasa medição e esconde estouro de custo — um sistema no seu fluxo fecha o ciclo no mesmo lugar.',
    bullets=['Cronograma e medições por obra em tempo real','Custos previstos × realizados sem digitação dupla','Diário de obra e fotos no celular da equipe','Relatórios prontos para o cliente e para o banco'],
    wa='Ola! Quero um diagnostico rapido de um sistema para minha empresa de engenharia',
    img='A Brazilian civil engineer reviewing a construction management dashboard on a tablet at a construction site office, cranes in background, realistic editorial photo'),
  dict(seg='advocacia', search_terms=['advocacia', 'advogados', 'juridico'], service='Site Profissional',
    subject='Quem procura advogado no Google encontra a {{empresa}}?',
    hook='Autoridade se constrói antes da primeira reunião.',
    subhead='Site profissional que posiciona a banca em quem pesquisa a sua área.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Cliente empresarial pesquisa a banca antes de ligar — um site sóbrio, rápido e bem posicionado no Google transmite o peso que a {{empresa}} tem no balcão.',
    bullets=['Áreas de atuação e equipe com autoridade','Artigos que posicionam a banca no Google','Agendamento de consulta pelo WhatsApp','Design sóbrio, rápido e impecável no celular'],
    wa='Ola! Quero um diagnostico rapido de um site para meu escritorio de advocacia',
    img='A Brazilian lawyer in a modern law office reviewing an elegant law firm website on a large monitor, bookshelves and city view, realistic editorial photo'),
  dict(seg='escola', search_terms=['escola', 'colegio', 'ensino'], service='App Escola-Familia',
    subject='Recados da {{empresa}} chegando lidos — não perdidos no grupo',
    hook='Agenda, recados e autorizações no celular dos pais. Lidos.',
    subhead='App próprio da escola: comunicação que a família realmente vê.',
    intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Grupo de WhatsApp engole recado importante — com app próprio, a escola envia agenda, autorização e boleto com confirmação de leitura.',
    bullets=['Recados com confirmação de leitura por turma','Agenda, eventos e autorizações digitais','Fotos do dia com privacidade controlada','Publicado nas lojas com a marca da escola'],
    wa='Ola! Quero um diagnostico rapido de um app para minha escola',
    img='A Brazilian school coordinator showing a school communication app on a smartphone to parents at a bright modern school entrance, realistic editorial photo'),
  dict(seg='marketing', search_terms=['marketing', 'publicidade', 'agencia'], service='Dev White-label',
    subject='{{empresa}}, e se a sua agência entregasse sites e apps sem contratar dev?',
    hook='Seu cliente pede site, app, sistema — eu entrego no seu nome.',
    subhead='Desenvolvimento white-label para agências: você vende, eu construo.',
    intro='Olá! Sou o Alexandre Queiroz, engenheiro de software. Agência perde receita quando o cliente pede tecnologia e ela só entrega mídia — em parceria white-label, você fecha o projeto e eu desenvolvo com o seu branding.',
    bullets=['Sites, landing pages, apps e sistemas no seu nome','Prazo e escopo fechados antes de você propor ao cliente','Você mantém o relacionamento; eu fico invisível','Margem sua em cada projeto entregue'],
    wa='Ola! Tenho uma agencia e quero saber mais sobre desenvolvimento white-label',
    img='A Brazilian marketing agency team presenting a website project to a client on a big screen in a creative modern office, realistic editorial photo'),
 ]),
 dict(wave=3, send_api='2026-07-27T14:30:00Z', send_sql='2026-07-27 14:30:00',
      file=DIR+'previews/wave-2026-07-27-w3.json', segments=[
  dict(seg='moveis', search_terms=['moveis', 'moveis planejados', 'marcenaria'], service='Site Catalogo',
    subject='O showroom da {{empresa}} aberto 24h — no Google',
    hook='Quem pesquisa móveis planejados precisa ver o seu projeto primeiro.',
    subhead='Site catálogo que transforma ambiente entregue em orçamento novo.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Móvel planejado se vende pelo projeto bonito — um site catálogo mostra seus ambientes para quem está pesquisando agora e leva direto pro orçamento no WhatsApp.',
    bullets=['Catálogo de ambientes em alta resolução','Aparecer no Google de quem busca planejados na cidade','Orçamento direto no WhatsApp a partir de cada projeto','Rápido e bonito no celular, com a sua marca'],
    wa='Ola! Quero um diagnostico rapido de um site catalogo para minha loja de moveis',
    img='A Brazilian custom furniture showroom owner showing a furniture catalog website on a tablet, elegant planned kitchen displays behind, realistic editorial photo'),
  dict(seg='academia', search_terms=['academia', 'crossfit', 'fitness'], service='App de Treino',
    subject='Alunos da {{empresa}} com treino no bolso — e matrícula em dia',
    hook='App próprio: treino, check-in e cobrança sem planilha.',
    subhead='Retenção começa no celular do aluno.',
    intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Aluno que sente acompanhamento fica — com app da academia, ele recebe treino, marca aula e paga sem você correr atrás.',
    bullets=['Fichas de treino atualizadas pelo professor','Check-in e agendamento de aulas pelo app','Cobrança recorrente com aviso de vencimento','Publicado nas lojas com a marca da academia'],
    wa='Ola! Quero um diagnostico rapido de um app para minha academia',
    img='A Brazilian gym owner showing a workout app on a smartphone to a member in a modern gym, equipment in background, realistic editorial photo'),
  dict(seg='otica', search_terms=['otica', 'oticas'], service='Site com Catalogo',
    subject='{{empresa}}: quem busca óculos na sua cidade te encontra online?',
    hook='Vitrine no Google: armações, lentes e agendamento de exame.',
    subhead='Site que coloca a ótica na frente de quem está pronto pra comprar.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Óculos começa com pesquisa no celular — um site com catálogo e agendamento de exame captura esse cliente antes do concorrente.',
    bullets=['Catálogo de armações e marcas em destaque','Agendamento de exame de vista online','Aparecer no Google de quem busca ótica na cidade','WhatsApp direto para orçamento de lentes'],
    wa='Ola! Quero um diagnostico rapido de um site para minha otica',
    img='A Brazilian optician showing eyewear frames and a store website on a tablet in a modern optical store with elegant glasses displays, realistic editorial photo'),
  dict(seg='pousada', search_terms=['pousada', 'hospedagem'], service='Site com Reservas',
    subject='Reserva direta na {{empresa}} — sem pagar comissão de plataforma',
    hook='Cada reserva direta é comissão que fica com você.',
    subhead='Site com motor de reservas próprio para a sua pousada.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites para hospedagem. Plataforma cobra comissão em toda reserva — um site bonito com reserva direta paga o investimento em poucas diárias.',
    bullets=['Motor de reservas direto no site, sem comissão','Fotos que vendem a experiência da pousada','Aparecer no Google de quem busca hospedagem na região','Integração com WhatsApp para dúvidas rápidas'],
    wa='Ola! Quero um diagnostico rapido de um site com reservas para minha pousada',
    img='A Brazilian guesthouse owner at a charming pousada reception showing a booking website on a laptop, tropical garden visible, realistic editorial photo'),
 ]),
]


def fetch_multi(search_terms, H, seen_e, seen_d, seen_c, want):
    out=[]
    for term in search_terms:
        if len(out)>=want: break
        leads,_=fetch_leads({'search':term}, H, seen_e, seen_d, seen_c, want-len(out))
        if leads:
            print(f'   search="{term}" -> {len(leads)}')
            out.extend(leads)
    return out[:want]


def reschedule(cid, when_sql):
    cx=crmdb.connect(autocommit=False); cur=cx.cursor()
    cur.execute("UPDATE email_queue_items SET scheduled_at=?, updated_at=SYSUTCDATETIME(), updated_by='fire_w2w3_2707' WHERE campaign_id=? AND status=0", when_sql, cid)
    n=cur.rowcount; cx.commit(); cx.close(); return n


def run():
    load_env(); H=login()
    seen_e, seen_d, seen_c = set(), set(), set()
    # preload dedupe com a w1 (queued, ainda nao-enviada — filtro nunca-contactado nao pega)
    w1=json.load(open(DIR+'previews/wave-2026-07-27-w1.json',encoding='utf-8'))
    import re
    for s in w1['segments']:
        for ld in s['leads']:
            em=(ld.get('email') or '').lower()
            if em: seen_e.add(em); seen_d.add(em.split('@',1)[1])
            ck=re.sub(r'[^a-z0-9]','',(ld.get('company') or '').lower())
            if ck: seen_c.add(ck)
    print(f'[{ts()}] preload w1: {len(seen_e)} emails, {len(seen_d)} dominios')

    for W in WAVES:
        print(f'\n[{ts()}] ===== WAVE {W["wave"]} — envia {W["send_sql"]} UTC =====')
        wave={'date':'2026-07-27','wave':W['wave'],'created':ts(),'template':'v2','segments':[]}
        for cfg in W['segments']:
            print(f'\n[{ts()}] --- {cfg["seg"].upper()} x {cfg["service"]} ---')
            leads=fetch_multi(cfg['search_terms'], H, seen_e, seen_d, seen_c, MAX_PER_SEG)
            if not leads: print('   0 leads — pulando'); continue
            leads=assign_providers(leads)
            print(f'[{ts()}]   {len(leads)} leads')
            img=gen_image(cfg['img'], H, keyword=cfg['seg'])
            if not img: print('   !!! sem imagem — pulando'); continue
            html=build_html_v2(cfg['hook'], cfg['subhead'], img, cfg['intro'], cfg['bullets'], cfg['wa'])
            open(DIR+f'templates/wave2707-{cfg["seg"]}.html','w',encoding='utf-8').write(html)
            H2=login()
            cr=requests.post(base+'/api/v1/email-campaigns/campaigns', headers=H2, timeout=30,
                json={'name': f'AQ - {cfg["seg"]} - {cfg["service"]} - 2026-07-27 w{W["wave"]}',
                      'subject': cfg['subject'], 'fromName':'Alexandre Queiroz',
                      'fromEmail':'contato@alexandrequeiroz.com.br', 'bodyHtml': html})
            if cr.status_code not in (200,201): print(f'   !!! CRIACAO {cr.status_code}: {cr.text[:200]}'); continue
            cid=cr.json().get('id') or cr.json().get('campaignId')
            sc=requests.post(base+f'/api/v1/email-campaigns/campaigns/{cid}/schedule', headers=H2,
                             json={'scheduledAt': W['send_api']}, timeout=30)
            for ld in leads: ld['campaignId']=cid
            wave['segments'].append({'seg':cfg['seg'],'service':cfg['service'],'campaignId':cid,'leads':leads})
            print(f'[{ts()}]   cid={cid[:8]} schedule={sc.status_code}')
        json.dump(wave, open(W['file'],'w',encoding='utf-8'), ensure_ascii=False, indent=2)
        total=sum(len(s['leads']) for s in wave['segments'])
        print(f'\n[{ts()}] W{W["wave"]} BUILD: {total} leads')
        if not total: continue
        if not ensure_breaker_closed(H): print('[ABORT] breaker aberto'); return
        H=login()
        for seg in wave['segments']:
            cid=seg['campaignId']
            payload=[{'customerId':ld['id'],'assignedProvider':ld['assignedProvider']} for ld in seg['leads']]
            r=requests.post(base+'/api/v1/email-providers/queue-with-assignment', headers=H, timeout=60,
                            json={'campaignId':cid,'leads':payload})
            jr=r.json() if r.status_code in (200,201,202) else {}
            moved=reschedule(cid, W['send_sql'])
            print(f'[{ts()}] {seg["seg"]:12} {seg["service"]:20} | QUEUE {r.status_code} queued={jr.get("queuedCount")} skip={jr.get("skippedCount")} | reagendados={moved}')
            if r.status_code not in (200,201,202): print('   err:', r.text[:200])
    print(f'\n[{ts()}] W2+W3 PRONTAS (segunda 10:30 e 11:30 BRT)')


if __name__ == '__main__':
    run()
