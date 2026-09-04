# -*- coding: utf-8 -*-
"""ORQUESTRADOR DIÁRIO de email marketing (Task Scheduler, seg-sex 08:40 BRT).

1. WAVES: monta até 3 ondas x 4 segmentos x 20 do estoque nunca-contactado,
   rotação de nichos (state em previews/daily-state.json), template v2,
   agendadas 09:00 / 10:30 / 11:30 BRT do próprio dia.
   Se a task rodar atrasada (PC dormindo), slots no passado são empurrados p/
   agora+10min mantendo gap de 60min; depois das 17:00 BRT não agenda (waves_lib).
2. FOLLOW-UP multi-toque, via POST /integrations/send-email (idempotente):
   FUP1 (4-14d após a wave, não clicou, cap 40) · FUP2 (7-21d após FUP1, cap 30)
   · FUP3 breakup (7-21d após FUP2, cap 20). Toda etapa exclui: clicou, respondeu,
   suprimido, opt-out, status CRM >= Qualified.
3. HOT LEADS: opens/clicks das últimas 24h -> digest no Telegram.
Guardas: breaker fechado, aborta se já houver >=50 na fila do dia, suppressions.
Uso: python daily_waves.py [--dry]  (--dry = só imprime o plano, não cria nada)
"""
import os, sys, json, re, datetime, urllib.parse, requests, html as _html

sys.path.insert(0, 'D:/claude-code/diax-crm/docs/email-marketing/')
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
from fire_today_20 import (DIR, base, HEALTHY, PER_PROVIDER, ts, load_env, login,
                           gen_image, fetch_leads, assign_providers, ensure_breaker_closed)
from fire_w1_2707 import build_html_v2, SEGMENTS as SEG_W1
from fire_w2w3_2707 import WAVES as W23
import db as crmdb
from waves_lib import (compute_wave_slots, brt_label, fup_candidates, is_role_mailbox, is_foreign_lead,
                       personalize_html, ACHADO_MARK)
from site_check import check_many, summarize_wave, summarize_fup
try:
    from copy_v3 import INTRO_V3, HOOK_V3   # intros que lideram com o mundo do leitor (skill cold-email)
except ImportError:
    INTRO_V3, HOOK_V3 = {}, {}

DRY = '--dry' in sys.argv
STATE_FILE = DIR + 'previews/daily-state.json'
LOG_DIR = DIR + 'logs/'
MAX_PER_SEG = PER_PROVIDER * len(HEALTHY)   # 20
# Slots-alvo 09:00/10:30/11:30 BRT vivem em waves_lib.WAVE_SLOTS_UTC (tolerantes a atraso).
FUP_CAP = 40                 # FUP1
FUP_CAPS = {2: 30, 3: 20}    # FUP2 / FUP3
WAVE_ERRORS = []             # campanhas que falharam em schedule/queue (p/ alerta)
MIN_SEG = 4  # menos que isso nao justifica campanha propria

# ---------- COPY BANK (rotação) ----------
def _harvest():
    bank = {}
    for cfg in SEG_W1: bank[cfg['seg']] = cfg
    for w in W23:
        for cfg in w['segments']: bank[cfg['seg']] = cfg
    return bank

COPY_BANK = _harvest()
COPY_BANK.update({
 'clinica': dict(seg='clinica', search_terms=['clinica', 'policlinica', 'centro medico'], service='Site com Agendamento',
    subject='Pacientes agendando na {{empresa}} — mesmo fora do horário',
    hook='Clínica que agenda online captura o paciente que pesquisa à noite.',
    subhead='Site profissional com agendamento integrado à sua agenda.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites para a área da saúde. Grande parte dos pacientes procura clínica fora do horário comercial — site com agendamento online captura essa consulta antes do concorrente.',
    bullets=['Agendamento online integrado à agenda da clínica','Aparecer no Google de quem busca a especialidade','Página que transmite confiança: equipe, estrutura, convênios','Lembretes que reduzem falta de paciente'],
    wa='Ola! Quero um diagnostico rapido de um site com agendamento para minha clinica',
    img='A Brazilian clinic receptionist showing an online booking website on a screen at a bright modern clinic reception, realistic editorial photo'),
 'loja': dict(seg='loja', search_terms=['loja', 'comercio', 'boutique'], service='Loja Online',
    subject='A {{empresa}} vende quando a porta está fechada?',
    hook='Catálogo online: sua loja aberta 24h no celular do cliente.',
    subhead='Site com catálogo e pedido no WhatsApp — simples e no ar rápido.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites e lojas online. Hoje o cliente pesquisa antes de sair de casa — quem tem catálogo online vende no WhatsApp todos os dias, inclusive fora do horário.',
    bullets=['Catálogo online sempre atualizado','Pedidos direto no WhatsApp','Aparecer no Google de quem busca na sua cidade','Rápido e bonito no celular, com a sua marca'],
    wa='Ola! Quero um diagnostico rapido de uma loja online para meu comercio',
    img='A Brazilian shop owner showing an online product catalog on a tablet at the counter of a modern retail store, realistic editorial photo'),
 'consultoria': dict(seg='consultoria', search_terms=['consultoria', 'assessoria'], service='Site de Autoridade',
    subject='A {{empresa}} parece tão boa no Google quanto é na reunião?',
    hook='Consultoria vende confiança — o site é a primeira reunião.',
    subhead='Site de autoridade que qualifica o cliente antes do primeiro contato.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Cliente de consultoria pesquisa antes de responder proposta — um site sólido com casos e método fecha a venda que a planilha de prospecção começou.',
    bullets=['Método e casos de sucesso em destaque','Artigos que posicionam a consultoria no Google','Agendamento de diagnóstico pelo WhatsApp','Design sóbrio e rápido, à altura da sua entrega'],
    wa='Ola! Quero um diagnostico rapido de um site para minha consultoria',
    img='A Brazilian business consultant presenting strategy on a screen to clients in a modern meeting room, professional atmosphere, realistic editorial photo'),
 'restaurante': dict(seg='restaurante', search_terms=['restaurante', 'pizzaria', 'churrascaria'], service='Site com Cardapio',
    subject='Quem procura onde almoçar acha a {{empresa}} — ou o vizinho?',
    hook='Cardápio online, reserva e pedido: seu restaurante no Google.',
    subhead='Site que transforma fome pesquisada em mesa ocupada.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Quem decide onde comer pesquisa no celular — site com cardápio atualizado e botão de reserva captura esse cliente na hora da fome.',
    bullets=['Cardápio online sempre atualizado','Reserva e pedido direto no WhatsApp','Aparecer no Google de quem busca perto','Fotos que dão fome, no celular e rápido'],
    wa='Ola! Quero um diagnostico rapido de um site para meu restaurante',
    img='A Brazilian restaurant owner showing an online menu website on a tablet in a cozy modern restaurant, plated dishes visible, realistic editorial photo'),
 'construtora': dict(seg='construtora', search_terms=['construtora', 'construcoes', 'incorporadora'], service='Site Institucional',
    subject='O próximo cliente da {{empresa}} pesquisou a obra no Google primeiro',
    hook='Portfólio de obras que vende a próxima antes da visita.',
    subhead='Site institucional com obras entregues, andamento e credibilidade.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Quem vai fechar uma obra pesquisa a construtora antes — site com portfólio de entregas e obra em andamento transmite a solidez que fecha contrato.',
    bullets=['Portfólio de obras entregues em destaque','Acompanhamento de obra para clientes','Aparecer no Google de quem busca construtora na região','Página de credibilidade: equipe, certidões, prazos'],
    wa='Ola! Quero um diagnostico rapido de um site para minha construtora',
    img='A Brazilian construction company owner reviewing a portfolio website with finished buildings on a laptop at a modern office, hard hat on desk, realistic editorial photo'),
 'informatica': dict(seg='informatica', search_terms=['informatica', 'tecnologia'], service='Parceria Dev',
    subject='{{empresa}}: cliente pedindo sistema que vocês não têm braço pra fazer?',
    hook='Seu cliente pede software — eu desenvolvo, você fatura junto.',
    subhead='Parceria de desenvolvimento para empresas de TI e informática.',
    intro='Olá! Sou o Alexandre Queiroz, engenheiro de software sênior. Quando seu cliente pede um sistema além do seu portfólio, em vez de recusar: eu desenvolvo em parceria, você mantém o cliente e a margem.',
    bullets=['Sistemas web, apps e integrações sob demanda','Escopo e prazo fechados antes de propor ao cliente','Você mantém o relacionamento comercial','Código documentado e entregue com garantia'],
    wa='Ola! Tenho uma empresa de TI e quero saber mais sobre parceria de desenvolvimento',
    img='Two Brazilian IT professionals reviewing code and system architecture on dual monitors in a modern tech office, realistic editorial photo'),
 'turismo': dict(seg='turismo', search_terms=['turismo', 'viagens', 'agencia de viagens'], service='Site com Pacotes',
    subject='O viajante pesquisa 3 sites antes de fechar. A {{empresa}} está entre eles?',
    hook='Pacotes bonitos no Google convertem sonho em reserva.',
    subhead='Site que mostra destinos e fecha pacote no WhatsApp.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Viajante compara online antes de fechar — site com pacotes atualizados e fotos que vendem o destino coloca sua agência na decisão final.',
    bullets=['Vitrine de pacotes e destinos sempre atualizada','Cotação e reserva direto no WhatsApp','Aparecer no Google de quem planeja viagem','Depoimentos de viajantes para gerar confiança'],
    wa='Ola! Quero um diagnostico rapido de um site para minha agencia de turismo',
    img='A Brazilian travel agent showing tropical destination packages on a large screen to excited clients in a bright travel agency, realistic editorial photo'),
 'geral_vitoria': dict(seg='geral_vitoria', search_terms=['vitoria', 'vila velha', 'serra', 'cariacica'], service='Presenca Digital',
    subject='{{empresa}}: quem procura seu serviço no Google encontra você ou o concorrente?',
    hook='Seu negócio aparecendo para quem procura na Grande Vitória.',
    subhead='Site profissional + Google: presença digital que traz cliente todo mês.',
    intro='Olá! Sou o Alexandre Queiroz, engenheiro de software capixaba. Todo dia alguém procura no Google exatamente o que a {{empresa}} faz — quem tem site profissional aparece primeiro e leva o contato.',
    bullets=['Site profissional com a cara do seu negócio','Aparecer no Google de quem busca na Grande Vitória','Contato direto no WhatsApp em um clique','No ar rápido, bonito no celular'],
    wa='Ola! Quero um diagnostico rapido de presenca digital para minha empresa',
    img='A Brazilian small business owner proudly showing a new professional website on a laptop in front of a local business storefront, realistic editorial photo'),
})

# Subjects v3 (skill cold-email: 2-4 palavras, lowercase, cara de email interno = +60% opens;
# nome da empresa no assunto = sinal de automação; pergunta genérica = -56%)
SHORT_SUBJ = {
 'logistica': 'controle de fretes', 'arquitetura': 'portfólio no google', 'estudio': 'agendamento das aulas',
 'medico': 'agenda do consultório', 'engenharia': 'medições da obra', 'advocacia': 'presença da banca',
 'escola': 'recados da escola', 'marketing': 'demanda de dev', 'moveis': 'catálogo de ambientes',
 'academia': 'retenção de alunos', 'otica': 'agendamento de exame', 'pousada': 'reservas diretas',
 'clinica': 'agenda da clínica', 'loja': 'catálogo online', 'consultoria': 'site da consultoria',
 'restaurante': 'cardápio online', 'construtora': 'portfólio de obras', 'informatica': 'parceria de dev',
 'turismo': 'pacotes no site', 'geral_vitoria': 'site da empresa',
}
for _k, _v in SHORT_SUBJ.items():
    if _k in COPY_BANK:
        COPY_BANK[_k]['subject'] = _v
# Copy v3 (copy_v3.py): intro/hook lideram com o mundo do leitor, não com "Sou o Alexandre".
for _k, _v in INTRO_V3.items():
    if _k in COPY_BANK and _v: COPY_BANK[_k]['intro'] = _v
for _k, _v in HOOK_V3.items():
    if _k in COPY_BANK and _v: COPY_BANK[_k]['hook'] = _v

# Variante B (A/B alternado por dia; comparação no relatório de sexta — campanha leva sufixo sA/sB)
SUBJ_B = {
 'logistica': 'fretes sem planilha', 'arquitetura': 'projetos no google', 'estudio': 'faltas nas aulas',
 'medico': 'consultas à noite', 'engenharia': 'custos da obra', 'advocacia': 'clientes empresariais',
 'escola': 'comunicação com pais', 'marketing': 'projetos de software', 'moveis': 'orçamentos de planejados',
 'academia': 'renovação de planos', 'otica': 'exame de vista', 'pousada': 'comissão das otas',
 'clinica': 'faltas de pacientes', 'loja': 'vendas no whatsapp', 'consultoria': 'novos clientes',
 'restaurante': 'reservas online', 'construtora': 'próxima obra', 'informatica': 'capacidade de dev',
 'turismo': 'cotações online', 'geral_vitoria': 'presença no google',
}

ROTATION = ['clinica', 'loja', 'geral_vitoria', 'consultoria', 'restaurante', 'construtora',
            'informatica', 'turismo', 'advocacia', 'engenharia', 'escola', 'marketing',
            'moveis', 'academia', 'otica', 'pousada', 'logistica', 'arquitetura', 'estudio', 'medico']

# ---------- infra ----------

def log_line(msg):
    os.makedirs(LOG_DIR, exist_ok=True)
    stamp = datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    line = f'[{stamp}] {msg}'
    try: print(line)
    except UnicodeEncodeError: print(line.encode('ascii','ignore').decode())
    with open(LOG_DIR + 'daily-waves-' + datetime.date.today().isoformat() + '.log', 'a', encoding='utf-8') as f:
        f.write(line + '\n')


def tg_send(text):
    try:
        tok = os.environ.get('TELEGRAM_BOT_TOKEN'); chat = os.environ.get('TELEGRAM_CHAT_ID')
        if not tok or not chat: return
        requests.post(f'https://api.telegram.org/bot{tok}/sendMessage',
                      json={'chat_id': chat, 'text': text, 'parse_mode': 'HTML'}, timeout=20)
    except Exception as e:
        log_line(f'telegram falhou: {e}')


def load_state():
    try: return json.load(open(STATE_FILE, encoding='utf-8'))
    except Exception: return {'cursor': 0, 'fup_sent': {}}


def save_state(st):
    json.dump(st, open(STATE_FILE, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)


def already_queued_today():
    cx = crmdb.connect(); cur = cx.cursor()
    cur.execute("SELECT COUNT(*) FROM email_queue_items WHERE CONVERT(date, scheduled_at)=CONVERT(date, SYSUTCDATETIME()) AND status=0")
    n = cur.fetchone()[0]; cx.close(); return n


def reschedule(cid, when_sql):
    cx = crmdb.connect(autocommit=False); cur = cx.cursor()
    cur.execute("UPDATE email_queue_items SET scheduled_at=?, updated_at=SYSUTCDATETIME(), updated_by='daily_waves' WHERE campaign_id=? AND status=0", when_sql, cid)
    n = cur.rowcount; cx.commit(); cx.close(); return n

def personalize_queue(cid, leads, site_res):
    """Após o enfileiramento: troca o marcador do html_body de CADA item pelo achado
    do site daquele lead (ou remove). O worker renderiza item.HtmlBody → vale por item.
    Falha no meio é inócua: o marcador é comentário HTML, invisível se sobrar.
    Idempotente (só toca itens que ainda têm o marcador)."""
    by_cust = {ld['id'].lower(): ld for ld in leads}
    cx = crmdb.connect(autocommit=False); cur = cx.cursor()
    cur.execute("SELECT id, customer_id, html_body FROM email_queue_items WHERE campaign_id=? AND status=0 AND html_body LIKE ?",
                cid, f'%{ACHADO_MARK}%')
    rows = cur.fetchall(); n = 0
    for r in rows:
        ld = by_cust.get(str(r.customer_id).lower())
        res = site_res.get((ld or {}).get('website') or '', {'findings': []})
        achado = summarize_wave(res['findings'], (ld or {}).get('company') or 'sua empresa')
        cur.execute("UPDATE email_queue_items SET html_body=?, updated_at=SYSUTCDATETIME(), updated_by='daily_waves_pers' WHERE id=?",
                    personalize_html(r.html_body, achado), r.id)
        n += 1 if achado else 0
    cx.commit(); cx.close()
    return n


# ---------- 1. WAVES ----------

def fetch_multi(terms, H, se, sd, sc, want):
    out = []
    for t in terms:
        if len(out) >= want: break
        leads, _ = fetch_leads({'search': t}, H, se, sd, sc, want - len(out))
        out.extend(leads or [])
    return out[:want]


def refresh_healthy(H):
    """Auto-heal: monta a lista de providers do dia lendo breaker-status.
    Provider com breaker aberto (ex: SendGrid sem créditos) sai da rotação
    automaticamente e volta sozinho quando o breaker fechar."""
    import fire_today_20 as ft
    # SendGrid FORA: free tier morto desde ago/2026 (total=0 créditos, hard limit) —
    # na fila (worker) provider é fixo sem fallback, item atribuído a ele morre.
    PREFER = ['Brevo', 'Mailjet', 'Resend', 'ElasticEmail', 'MailerSend']
    try:
        r = requests.get(base + '/api/v1/email-providers/breaker-status', headers=H, timeout=30).json()
        enabled = set(r.get('enabledProviders') or [])
        open_breakers = {p['provider'].lower() for p in r.get('providers', []) if p.get('isOpen')}
        healthy = [p for p in PREFER if p in enabled and p.lower() not in open_breakers][:4]
        if len(healthy) >= 2:
            removed = [p for p in ft.HEALTHY if p not in healthy]
            ft.HEALTHY[:] = healthy  # in-place: assign_providers usa a mesma lista
            if removed:
                log_line(f'auto-heal: fora da rotação: {removed} | ativos: {healthy}')
                tg_send(f'⚙️ Provider(s) fora da rotação hoje: {", ".join(removed)} (breaker aberto). Ativos: {", ".join(healthy)}')
        return len(ft.HEALTHY)
    except Exception as e:
        log_line(f'refresh_healthy falhou (mantém default): {e}')
        return len(ft.HEALTHY)


def build_waves(H, st):
    today = datetime.date.today().isoformat()
    n_prov = refresh_healthy(H)
    global MAX_PER_SEG
    MAX_PER_SEG = PER_PROVIDER * n_prov
    guard = already_queued_today()
    if guard >= 50:
        log_line(f'GUARD: {guard} itens já agendados hoje — pulando waves'); return []
    # Horários tolerantes a atraso: task rodou 13:13 em 03/09 (PC dormindo) e o
    # /schedule recusou 09:00 (passado) -> 0 emails. Calcula ANTES do fetch (caro).
    now_utc = datetime.datetime.utcnow()
    slots = compute_wave_slots(now_utc)
    if not slots:
        log_line(f'TARDE DEMAIS ({brt_label(now_utc)} BRT) — sem waves hoje')
        if not DRY:
            tg_send(f'⚠️ Email waves: task rodou às {brt_label(now_utc)} BRT (após 17:00) — sem envio hoje. Ver PC/task.')
            return []
        log_line('DRY: ignorando cutoff para montar o plano')
    if slots and slots[0] != datetime.datetime.combine(now_utc.date(), datetime.time(12, 0)):
        log_line(f'ATRASO: agora {brt_label(now_utc)} BRT -> ondas em {[brt_label(s) for s in slots]} BRT')
    se, sd, sc = set(), set(), set()
    plan, cursor = [], st.get('cursor', 0)
    tried = 0
    while len(plan) < 12 and tried < len(ROTATION):
        niche = ROTATION[(cursor + tried) % len(ROTATION)]; tried += 1
        cfg = COPY_BANK[niche]
        leads = fetch_multi(cfg['search_terms'], H, se, sd, sc, MAX_PER_SEG)
        if len(leads) < MIN_SEG:
            log_line(f'  {niche}: {len(leads)} (<{MIN_SEG}) — pulo'); continue
        plan.append((cfg, leads))
        log_line(f'  {niche}: {len(leads)} leads OK')
    st['cursor'] = (cursor + tried) % len(ROTATION)

    # Diagnóstico real do site de cada lead (cache 30d) → 1 frase personalizada por email.
    site_res = {}
    try:
        urls = [ld.get('website') or '' for _, leads in plan for ld in leads]
        t0 = datetime.datetime.now()
        site_res = check_many(urls, workers=8)
        n_ach = sum(1 for r in site_res.values() if r['findings'])
        log_line(f'site-check: {len(site_res)} sites em {(datetime.datetime.now()-t0).seconds}s — {n_ach} com achado')
    except Exception as e:
        log_line(f'site-check falhou (emails saem sem achado): {e}')

    if DRY:
        log_line(f'DRY: plano = {[(c["seg"], len(l)) for c, l in plan]}')
        for cfg, leads in plan[:2]:
            for ld in leads[:3]:
                r = site_res.get(ld.get('website') or '', {'findings': []})
                log_line(f'  DRY achado {ld["email"]}: {summarize_wave(r["findings"], ld["company"] or "empresa")[:110] or "(site ok)"}')
        return []

    # A/B de subject: alterna a variante por dia; sufixo sA/sB no nome da campanha
    # permite comparar opens no relatório de sexta.
    variant = 'B' if st.get('ab_flip') else 'A'
    st['ab_flip'] = not st.get('ab_flip')
    fired = []
    for i, (cfg, leads) in enumerate(plan):
        wave_idx = i // 4
        # Recalcula a cada campanha: fetch + geração de imagem + criação consomem
        # minutos; um slot calculado no início pode já estar no passado aqui
        # (compute_wave_slots é monotônica: nunca devolve slot anterior ao já usado).
        slots = compute_wave_slots(datetime.datetime.utcnow())
        if wave_idx >= len(slots):
            log_line(f'  {cfg["seg"]}: sem slot restante hoje (cutoff) — fica p/ amanhã'); break
        when_dt = slots[wave_idx]; brt = brt_label(when_dt)
        when_api = when_dt.strftime('%Y-%m-%dT%H:%M:%SZ'); when_sql = when_dt.strftime('%Y-%m-%d %H:%M:%S')
        leads = assign_providers(leads)
        subject = SUBJ_B.get(cfg['seg'], cfg['subject']) if variant == 'B' else cfg['subject']
        img = gen_image(cfg['img'], H, keyword=cfg['seg'])
        if not img: log_line(f'  {cfg["seg"]}: sem imagem — pulo'); continue
        # marcador no fim da intro: vira o achado do site por item (personalize_queue) ou some
        html = build_html_v2(cfg['hook'], cfg['subhead'], img, cfg['intro'] + ACHADO_MARK, cfg['bullets'], cfg['wa'])
        H2 = login()
        cr = requests.post(base + '/api/v1/email-campaigns/campaigns', headers=H2, timeout=30,
                           json={'name': f'AQ - {cfg["seg"]} - {cfg["service"]} - {today} auto s{variant}',
                                 'subject': subject, 'fromName': 'Alexandre Queiroz',
                                 'fromEmail': 'contato@alexandrequeiroz.com.br', 'bodyHtml': html})
        if cr.status_code not in (200, 201):
            log_line(f'  {cfg["seg"]}: criacao {cr.status_code} {cr.text[:150]}'); continue
        cid = cr.json().get('id') or cr.json().get('campaignId')
        # /schedule tira a campanha de Draft (readiness gate do queue). Se falhar
        # (ex.: horário no passado -> 400), o queue também falha: não insistir.
        sr = requests.post(base + f'/api/v1/email-campaigns/campaigns/{cid}/schedule', headers=H2,
                           json={'scheduledAt': when_api}, timeout=30)
        if sr.status_code not in (200, 201):
            log_line(f'  {cfg["seg"]}: schedule {sr.status_code} {sr.text[:200]} (when={when_api})')
            WAVE_ERRORS.append(f'{cfg["seg"]} schedule {sr.status_code}'); continue
        payload = [{'customerId': ld['id'], 'assignedProvider': ld['assignedProvider']} for ld in leads]
        r = requests.post(base + '/api/v1/email-providers/queue-with-assignment', headers=H2, timeout=60,
                          json={'campaignId': cid, 'leads': payload})
        if r.status_code not in (200, 201, 202):
            log_line(f'  {cfg["seg"]}: queue {r.status_code} {r.text[:200]}')
            WAVE_ERRORS.append(f'{cfg["seg"]} queue {r.status_code}'); continue
        jr = r.json()
        moved = reschedule(cid, when_sql)
        try:
            pers = personalize_queue(cid, leads, site_res)
        except Exception as e:
            pers = -1; log_line(f'  {cfg["seg"]}: personalize falhou (itens saem com intro padrão): {e}')
        log_line(f'  {cfg["seg"]:14} personalizados={pers}')
        fired.append({'seg': cfg['seg'], 'n': jr.get('queuedCount') or 0, 'brt': brt})
        log_line(f'  {cfg["seg"]:14} queue={r.status_code} n={jr.get("queuedCount")} skip={jr.get("skippedCount")} sched {brt} BRT (reag {moved})')
    return fired

# ---------- 2. FOLLOW-UP ----------

FUP_HTML = ('<div style="font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:24px;color:#1f2937;max-width:560px;">'
 '<p>Ol&aacute;{nome}!</p>'
 '<p>Semana passada enviei uma ideia de <strong>{service}</strong> para a <strong>{empresa}</strong> e n&atilde;o sei se chegou a ver.</p>'
 '<p>Faz sentido para voc&ecirc;s agora? O diagn&oacute;stico &eacute; gratuito e sem compromisso &mdash; &eacute; s&oacute; me chamar no '
 '<a href="https://wa.me/5527999840101?text={wa}">WhatsApp (27) 99984-0101</a>.</p>'
 '<p>Se n&atilde;o for o momento, sem problema nenhum &mdash; me diz e n&atilde;o insisto.</p>'
 '<p>Abra&ccedil;o,<br/><strong>Alexandre Queiroz</strong><br/>Engenheiro de software &middot; alexandrequeiroz.com.br</p></div>')


def run_followups(st):
    key = os.environ.get('DIAX_SEND_EMAIL_KEY')
    if not key: log_line('FUP: sem DIAX_SEND_EMAIL_KEY — pulo'); return 0
    cx = crmdb.connect(); cur = cx.cursor()
    cur.execute("""
      SELECT TOP 80 q.recipient_email, q.recipient_name, q.subject, c.name AS camp_name
      FROM email_queue_items q
      JOIN email_campaigns c ON c.id = q.campaign_id
      WHERE q.status = 2
        AND q.sent_at BETWEEN DATEADD(day,-14,SYSUTCDATETIME()) AND DATEADD(hour,-90,SYSUTCDATETIME())
        AND c.name LIKE 'AQ - %'
        AND NOT EXISTS (SELECT 1 FROM email_suppressions s WHERE s.email = q.recipient_email)
        AND NOT EXISTS (SELECT 1 FROM email_events e WHERE e.queue_item_id = q.id AND e.event_type = 3)
        AND NOT EXISTS (SELECT 1 FROM customers c2 WHERE c2.email = q.recipient_email AND c2.email_opt_out = 1)
      ORDER BY q.sent_at ASC""")
    rows = cur.fetchall(); cx.close()
    sent = 0
    for r in rows:
        if sent >= FUP_CAP: break
        em = (r.recipient_email or '').lower()
        if not em or em in st['fup_sent']: continue
        m = re.match(r'AQ - (\w+) - ([^-]+) -', r.camp_name or '')
        service = (m.group(2).strip() if m else 'site profissional')
        empresa = r.recipient_name or 'sua empresa'
        nome = ''
        wa = urllib.parse.quote(f'Ola! Recebi seu email sobre {service}. Quero saber mais.')
        body = FUP_HTML.format(nome=nome, service=service, empresa=empresa, wa=wa)
        # mesmo subject do original → Gmail agrupa como conversa ("Re:" falso é anti-padrão)
        subj = (r.subject or f'{service} para sua empresa')[:140]
        if DRY:
            log_line(f'DRY FUP -> {em} | {subj[:60]}'); sent += 1; continue
        resp = requests.post(base + '/api/v1/integrations/send-email',
            headers={'X-Integration-Key': key, 'Content-Type': 'application/json'}, timeout=40,
            json={'fromEmail': 'contato@alexandrequeiroz.com.br',
                  'fromName': 'Alexandre Queiroz',
                  'to': [{'address': em, 'display': empresa}],
                  'subject': subj, 'html': body,
                  'tags': ['fup1'],
                  'allowUnaligned': False,
                  # sem data: 1 FUP1 por destinatário, mesmo se o state não for salvo (crash)
                  'idempotencyKey': f'fup1-{em}'})
        ok = resp.status_code in (200, 201, 202)
        if ok:
            st['fup_sent'][em] = datetime.date.today().isoformat(); sent += 1
        log_line(f'FUP {em}: {resp.status_code}')
    return sent


# FUP2 (7-21d após FUP1): ângulo novo — oferta concreta e de baixo atrito.
FUP2_HTML = ('<div style="font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:24px;color:#1f2937;max-width:560px;">'
 '<p>Ol&aacute;!</p>'
 '<p>Alexandre de novo &mdash; prometo ser breve. J&aacute; escrevi sobre <strong>{service}</strong> para a <strong>{empresa}</strong>, '
 'ent&atilde;o desta vez, em vez de falar, fui olhar.</p>'
 '{analise}'
 '<p>S&atilde;o ajustes que d&atilde;o para resolver em uma semana e costumam trazer mais contato de cliente. '
 'Se quiser, eu explico em 5 minutos no <a href="https://wa.me/5527999840101?text={wa}">WhatsApp (27) 99984-0101</a> &mdash; '
 'ou responde este email com <strong>&ldquo;pode explicar&rdquo;</strong>. Sem compromisso e sem &ldquo;apresenta&ccedil;&atilde;o de vendas&rdquo;.</p>'
 '<p>Abra&ccedil;o,<br/><strong>Alexandre Queiroz</strong><br/>Engenheiro de software &middot; alexandrequeiroz.com.br</p></div>')

# FUP3 (7-21d após FUP2): breakup — encerra a sequência com porta aberta.
FUP3_HTML = ('<div style="font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:24px;color:#1f2937;max-width:560px;">'
 '<p>Ol&aacute;!</p>'
 '<p>Este &eacute; o meu &uacute;ltimo email sobre <strong>{service}</strong> para a <strong>{empresa}</strong> &mdash; '
 'n&atilde;o quero ser inconveniente.</p>'
 '<p>Se em algum momento fizer sentido ter um site ou sistema que traga clientes de verdade, &eacute; s&oacute; me chamar no '
 '<a href="https://wa.me/5527999840101?text={wa}">WhatsApp (27) 99984-0101</a>. Fico &agrave; disposi&ccedil;&atilde;o.</p>'
 '<p>Desejo &oacute;timos neg&oacute;cios para a {empresa}!</p>'
 '<p>Abra&ccedil;o,<br/><strong>Alexandre Queiroz</strong><br/>Engenheiro de software &middot; alexandrequeiroz.com.br</p></div>')

FUP_STAGES = {
    2: dict(prev='fup_sent',  key='fup2_sent', html=FUP2_HTML, tag='fup2', subj='Uma sugestão rápida para a {empresa}'),
    3: dict(prev='fup2_sent', key='fup3_sent', html=FUP3_HTML, tag='fup3', subj='Último email — {empresa}'),
}


def fup_exclusions(emails):
    """Quem NÃO deve receber mais toques: clicou, suprimido, opt-out, ou já avançou
    no funil (status >= Qualified — P1b marca clicadores como Qualified)."""
    if not emails: return set()
    cx = crmdb.connect(); cur = cx.cursor()
    out = set()
    ems = list(emails)
    for i in range(0, len(ems), 500):
        chunk = ems[i:i+500]; marks = ','.join('?' * len(chunk))
        cur.execute(f"""
          SELECT LOWER(email) FROM email_suppressions WHERE LOWER(email) IN ({marks})
          UNION SELECT LOWER(email) FROM customers WHERE LOWER(email) IN ({marks}) AND (email_opt_out=1 OR status>=2)
          UNION SELECT DISTINCT LOWER(q.recipient_email) FROM email_events e JOIN email_queue_items q ON q.id=e.queue_item_id
                WHERE e.event_type=3 AND LOWER(q.recipient_email) IN ({marks})""", *chunk, *chunk, *chunk)
        out.update(r[0] for r in cur.fetchall())
    cx.close()
    return out


def fup_context(emails):
    """{email: (empresa, service)} a partir da última wave AQ enviada p/ o email."""
    if not emails: return {}
    cx = crmdb.connect(); cur = cx.cursor()
    ctx = {}
    ems = list(emails)
    for i in range(0, len(ems), 500):
        chunk = ems[i:i+500]; marks = ','.join('?' * len(chunk))
        cur.execute(f"""
          SELECT LOWER(q.recipient_email) em, MAX(q.recipient_name) nome, MAX(c.name) camp,
                 MAX(cu.website) website
          FROM email_queue_items q JOIN email_campaigns c ON c.id=q.campaign_id
          LEFT JOIN customers cu ON cu.id=q.customer_id
          WHERE q.status=2 AND c.name LIKE 'AQ - %' AND LOWER(q.recipient_email) IN ({marks})
          GROUP BY LOWER(q.recipient_email)""", *chunk)
        for r in cur.fetchall():
            m = re.match(r'AQ - (\w+) - ([^-]+) -', r.camp or '')
            ctx[r.em] = (r.nome or 'sua empresa', (m.group(2).strip() if m else 'site profissional'), (r.website or '').strip())
    cx.close()
    return ctx


def run_fup_stage(st, stage, replies):
    """FUP2/FUP3: candidatos vêm do state da etapa anterior (email -> data)."""
    cfg = FUP_STAGES[stage]
    key = os.environ.get('DIAX_SEND_EMAIL_KEY')
    if not key: log_line(f'FUP{stage}: sem DIAX_SEND_EMAIL_KEY — pulo'); return 0
    prev = st.get(cfg['prev']) or {}
    done = st.setdefault(cfg['key'], {})
    today = datetime.date.today()
    pool = fup_candidates(prev, done, today, 7, 21, set())
    if not pool: return 0
    excl = (fup_exclusions(pool) | {e.lower() for e in replies}
            | {e for e in pool if is_role_mailbox(e) or is_foreign_lead(e)})
    cands = [e for e in pool if e not in excl][:FUP_CAPS[stage]]
    log_line(f'FUP{stage}: {len(pool)} na janela, {len(pool)-len([e for e in pool if e not in excl])} excluídos, {len(cands)} a enviar')
    if not cands: return 0
    ctx = fup_context(cands)
    site_res = {}
    if stage == 2:   # a "análise gratuita" é entregue de verdade: achados reais do site
        try:
            site_res = check_many([ctx[e][2] for e in cands if e in ctx], workers=8)
        except Exception as e:
            log_line(f'FUP2 site-check falhou: {e}')
    sent = 0
    for em in cands:
        empresa, service, website = ctx.get(em, ('sua empresa', 'site profissional', ''))
        if is_foreign_lead(em, empresa):
            log_line(f'FUP{stage} {em}: pulado (lead estrangeiro: {empresa[:40]})'); continue
        wa = urllib.parse.quote(f'Ola! Recebi seu email sobre {service}. Quero saber mais.')
        analise = ''
        if stage == 2:
            res = site_res.get(website)          # None = check não rodou/falhou → NÃO afirmar nada
            items = summarize_fup(res['findings'], empresa) if res else None
            if items:
                head = {1: 'Uma coisa que notei', 2: 'Duas coisas que notei', 3: 'Tr&ecirc;s coisas que notei'}[len(items)]
                enc = lambda s: _html.escape(s[0].upper() + s[1:] + '.', quote=True).encode('ascii', 'xmlcharrefreplace').decode()
                analise = (f'<p>{head} no site de voc&ecirc;s:</p><ol style="padding-left:20px;margin:0 0 16px 0;">'
                           + ''.join(f'<li style="margin-bottom:6px;">{enc(i)}</li>' for i in items) + '</ol>')
            elif items == []:                    # verificado e ok: muda o ângulo, sem inventar defeito
                analise = ('<p>O site de voc&ecirc;s est&aacute; bem cuidado &mdash; abre r&aacute;pido, com cadeado e no celular. '
                           'Ent&atilde;o o que costuma faltar &eacute; o pr&oacute;ximo passo: transformar quem entra em contato '
                           '(agendamento, or&ccedil;amento ou cat&aacute;logo direto no WhatsApp).</p>')
            else:                                # não verificável (bloqueio/falha): oferta neutra
                analise = ('<p>Em vez de pedir uma reuni&atilde;o, prefiro propor algo concreto: uma <strong>an&aacute;lise r&aacute;pida e '
                           'gratuita</strong> de como a {empresa} aparece hoje no Google e no celular, com 2 ou 3 ajustes pr&aacute;ticos.</p>'
                           ).replace('{empresa}', _html.escape(empresa, quote=True).encode('ascii', 'xmlcharrefreplace').decode())
        body = cfg['html'].format(service=service, empresa=empresa, wa=wa, analise=analise)
        subj = cfg['subj'].format(empresa=empresa)[:140]
        if DRY:
            if sent == 0:
                open(DIR + f'previews/fup{stage}-preview.html', 'w', encoding='utf-8').write(body)
            log_line(f'DRY FUP{stage} -> {em} | {subj[:60]}'); sent += 1; continue
        resp = requests.post(base + '/api/v1/integrations/send-email',
            headers={'X-Integration-Key': key, 'Content-Type': 'application/json'}, timeout=40,
            json={'fromEmail': 'contato@alexandrequeiroz.com.br', 'fromName': 'Alexandre Queiroz',
                  'to': [{'address': em, 'display': empresa}], 'subject': subj, 'html': body,
                  'tags': [cfg['tag']], 'allowUnaligned': False,
                  'idempotencyKey': f'{cfg["tag"]}-{em}'})   # estável: 1 por etapa/destinatário
        if resp.status_code in (200, 201, 202):
            done[em] = today.isoformat(); sent += 1
        log_line(f'FUP{stage} {em}: {resp.status_code}')
    return sent

# ---------- 3. HOT LEADS ----------

def sync_bounce_suppressions():
    """Rede de segurança: bounce (evento 4) últimos 7d sem suppression → insere.
    Webhooks cobrem a maioria; isso pega o que escapou (providers sem webhook)."""
    cx = crmdb.connect(autocommit=False); cur = cx.cursor()
    cur.execute("""
      INSERT INTO email_suppressions (id, user_id, email, reason, source, suppressed_at, created_at, created_by)
      SELECT NEWID(), q.user_id, q.recipient_email, 0, 'daily_waves_bounce_sync', SYSUTCDATETIME(), SYSUTCDATETIME(), 'daily_waves'
      FROM (SELECT DISTINCT q.user_id, q.recipient_email FROM email_events e
            JOIN email_queue_items q ON q.id=e.queue_item_id
            WHERE e.event_type=4 AND e.occurred_at >= DATEADD(day,-7,SYSUTCDATETIME())) q
      WHERE NOT EXISTS (SELECT 1 FROM email_suppressions s WHERE s.email = q.recipient_email)""")
    n = cur.rowcount; cx.commit(); cx.close()
    if n: log_line(f'bounce-sync: {n} suppressions adicionadas')
    return n


def scan_replies():
    """IMAP no contato@ (HostGator): remetentes das últimas 48h que estão entre os
    destinatários de waves → RESPONDEU (a conversão real). Read-only (BODY.PEEK)."""
    import imaplib
    user = os.environ.get('HOSTGATOR_CONTATO_EMAIL'); pwd = os.environ.get('HOSTGATOR_CONTATO_PASS')
    if not user or not pwd:
        return []
    try:
        M = imaplib.IMAP4_SSL('mail.alexandrequeiroz.com.br', 993)
        M.login(user, pwd); M.select('INBOX', readonly=True)
        since = (datetime.date.today() - datetime.timedelta(days=2)).strftime('%d-%b-%Y')
        _, data = M.search(None, f'(SINCE {since})')
        ids = data[0].split()[-80:]
        senders = set()
        for i in ids:
            _, msg = M.fetch(i, '(BODY.PEEK[HEADER.FIELDS (FROM)])')
            raw = (msg[0][1] or b'').decode(errors='ignore')
            m = re.search(r'[\w.+-]+@[\w.-]+', raw)
            if m:
                em = m.group(0).lower()
                # ignora nosso próprio domínio e remetentes automáticos (falso positivo)
                if em.endswith('@alexandrequeiroz.com.br') or any(x in em for x in ('noreply', 'no-reply', 'mailer-daemon', 'postmaster', 'bounce')):
                    continue
                senders.add(em)
        M.logout()
        if not senders:
            return []
        cx = crmdb.connect(); cur = cx.cursor()
        marks = ','.join('?' * len(senders))
        cur.execute(f"SELECT DISTINCT recipient_email FROM email_queue_items WHERE status=2 AND recipient_email IN ({marks})", *senders)
        hits = [r[0] for r in cur.fetchall()]
        cx.close()
        return hits
    except Exception as e:
        log_line(f'reply-scan falhou: {e}')
        return []


def create_crm_task(H, title, desc, priority=3, due_days=1):
    """Cria tarefa no CRM → cai no Daily Briefing do Alexandre. Priority: 3=High 4=Urgent."""
    try:
        due = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(days=due_days)).strftime('%Y-%m-%dT12:00:00Z')
        r = requests.post(base + '/api/v1/tasks', headers=H, timeout=30,
                          json={'title': title[:180], 'description': desc[:1500], 'priority': priority, 'dueDate': due})
        return r.status_code in (200, 201)
    except Exception as e:
        log_line(f'create_crm_task falhou: {e}'); return False


def sync_hot_to_crm(H, hot, replies, st):
    """Respostas = Urgent; clicks/2+opens = High. Dedupe via state (1 task por email)."""
    done = st.setdefault('crm_tasks', {})
    created = 0
    for em in replies:
        if em in done: continue
        if create_crm_task(H, f'📧 RESPONDEU wave: {em}', f'Lead respondeu email de prospecção. Responder HOJE: {em}', priority=4, due_days=0):
            done[em] = datetime.date.today().isoformat(); created += 1
    for r in hot:
        em = r.recipient_email
        if em in done: continue
        why = f'{r.clicks or 0} click(s), {r.opens or 0} opens'
        if create_crm_task(H, f'🔥 Lead quente wave: {r.name or em}', f'{em} — {why}. Ligar/WhatsApp em 24h (janela quente).', priority=3, due_days=1):
            done[em] = datetime.date.today().isoformat(); created += 1
    return created


def stock_runway(H):
    """Estima estoque nunca-contactado alcançável; alarme se < ~3 dias de cadência."""
    se, sd, sc = set(), set(), set()
    total = 0
    for niche in ROTATION[:8]:
        cfg = COPY_BANK[niche]
        leads = fetch_multi(cfg['search_terms'], H, se, sd, sc, MAX_PER_SEG)
        total += len(leads)
    return total  # amostra dos 8 primeiros nichos da rotação


def db_housekeeping():
    """Sexta: purge preventivo — o banco SmarterASP tem quota (~500MB) e audit_logs
    encheu tudo em 30/07 (worker+endpoints 500 por filegroup FULL). Nunca mais."""
    cx = crmdb.connect(autocommit=True); cur = cx.cursor()
    freed = {}
    cur.execute("DELETE TOP (5000) FROM audit_logs WHERE timestamp_utc < DATEADD(day,-30,SYSUTCDATETIME())")
    freed['audit_logs'] = cur.rowcount
    cur.execute("DELETE TOP (5000) FROM app_logs WHERE timestamp_utc < DATEADD(day,-14,SYSUTCDATETIME())")
    freed['app_logs'] = cur.rowcount
    cur.execute("UPDATE email_queue_items SET html_body='[purged]' WHERE status=2 AND sent_at < DATEADD(day,-7,SYSUTCDATETIME()) AND html_body<>'[purged]'")
    freed['html_body'] = cur.rowcount
    cur.execute("SELECT CAST(SUM(size)*8/1024.0 AS DECIMAL(10,1)) FROM sys.database_files")
    size = float(cur.fetchone()[0]); cx.close()
    log_line(f'housekeeping: {freed} | DB {size} MB')
    return freed, size


def weekly_report():
    """Sexta: performance da semana (envios, opens, clicks, bounces, por campanha top5)."""
    cx = crmdb.connect(); cur = cx.cursor()
    cur.execute("""
      SELECT COUNT(*) sent FROM email_queue_items WHERE status=2 AND sent_at >= DATEADD(day,-7,SYSUTCDATETIME())""")
    sent = cur.fetchone()[0]
    cur.execute("""
      SELECT e.event_type, COUNT(DISTINCT e.queue_item_id) FROM email_events e
      WHERE e.occurred_at >= DATEADD(day,-7,SYSUTCDATETIME()) GROUP BY e.event_type""")
    ev = {r[0]: r[1] for r in cur.fetchall()}
    cur.execute("""
      SELECT TOP 5 c.name, COUNT(DISTINCT q.id) n,
             COUNT(DISTINCT CASE WHEN e.event_type=2 THEN e.queue_item_id END) opens
      FROM email_queue_items q JOIN email_campaigns c ON c.id=q.campaign_id
      LEFT JOIN email_events e ON e.queue_item_id=q.id
      WHERE q.sent_at >= DATEADD(day,-7,SYSUTCDATETIME())
      GROUP BY c.name ORDER BY opens DESC""")
    top = cur.fetchall(); cx.close()
    opens, clicks, bounces = ev.get(2, 0), ev.get(3, 0), ev.get(4, 0)
    orate = f'{opens/sent*100:.1f}%' if sent else '—'
    lines = '\n'.join(f'  {r.name[:44]}: {r.opens}/{r.n} opens' for r in top)
    return (f'📊 <b>Semana de emails</b>\nEnviados: {sent} | Opens: {opens} ({orate}) | Clicks: {clicks} | Bounces: {bounces}\n'
            f'Top campanhas:\n{lines}')


def hot_leads_digest():
    cx = crmdb.connect(); cur = cx.cursor()
    cur.execute("""
      SELECT q.recipient_email, MAX(q.recipient_name) name,
             SUM(CASE WHEN e.event_type=2 THEN 1 ELSE 0 END) opens,
             SUM(CASE WHEN e.event_type=3 THEN 1 ELSE 0 END) clicks
      FROM email_events e JOIN email_queue_items q ON q.id = e.queue_item_id
      WHERE e.occurred_at >= DATEADD(hour,-24,SYSUTCDATETIME()) AND e.event_type IN (2,3)
      GROUP BY q.recipient_email
      ORDER BY SUM(CASE WHEN e.event_type=3 THEN 1 ELSE 0 END) DESC,
               SUM(CASE WHEN e.event_type=2 THEN 1 ELSE 0 END) DESC""")
    rows = cur.fetchall(); cx.close()
    hot = [r for r in rows if (r.clicks or 0) > 0 or (r.opens or 0) >= 2]
    return rows, hot


def main():
    if datetime.date.today().weekday() >= 5 and '--force' not in sys.argv:
        print('fim de semana — sem waves (use --force para ignorar)'); return
    load_env()
    st = load_state()
    log_line(f'===== DAILY WAVES {"(DRY) " if DRY else ""}=====')
    H = login()
    if not ensure_breaker_closed(H):
        log_line('ABORT: breaker aberto'); tg_send('⚠️ Email waves ABORTADAS: circuit breaker aberto'); return
    fired = build_waves(H, st)
    replies = scan_replies()          # antes dos FUPs: quem respondeu não recebe mais toque
    fups = run_followups(st)
    fup2 = run_fup_stage(st, 2, replies)
    fup3 = run_fup_stage(st, 3, replies)
    if not DRY: sync_bounce_suppressions()
    rows, hot = hot_leads_digest()
    crm_tasks = 0 if DRY else sync_hot_to_crm(H, hot, replies, st)
    if not DRY: save_state(st)
    total = sum(f['n'] for f in fired)
    seg_txt = ' · '.join(f"{f['seg']}({f['n']})@{f['brt']}" for f in fired) or '—'
    hot_txt = '\n'.join(f"  🔥 {r.name or r.recipient_email} — {r.clicks or 0} click, {r.opens or 0} opens" for r in hot[:10]) or '  (nenhum)'
    msg = (f'📧 <b>Email waves {datetime.date.today().strftime("%d/%m")}</b>\n'
           f'Agendados hoje: <b>{total}</b>\n{seg_txt}\n'
           f'Follow-ups: FUP1 <b>{fups}</b> · FUP2 <b>{fup2}</b> · FUP3 <b>{fup3}</b>\n'
           f'Leads quentes 24h ({len(hot)}):\n{hot_txt}'
           + (('\n💬 <b>RESPONDERAM (ligar!):</b>\n' + '\n'.join(f'  {e}' for e in replies[:10])) if replies else ''))
    if WAVE_ERRORS:
        msg += f'\n🚨 <b>{len(WAVE_ERRORS)} campanha(s) FALHARAM</b> (schedule/queue): ' + ', '.join(WAVE_ERRORS[:6])
    if crm_tasks:
        msg += f'\n📋 Tarefas criadas no CRM: {crm_tasks}'
    # sexta: relatório semanal + medição de pista
    if datetime.date.today().weekday() == 4:
        try:
            msg += '\n\n' + weekly_report()
        except Exception as e:
            log_line(f'weekly_report falhou: {e}')
        try:
            freed, dbsize = db_housekeeping()
            msg += f'\n🧹 DB: {dbsize:.0f} MB (purge: audit {freed["audit_logs"]}, logs {freed["app_logs"]}, bodies {freed["html_body"]})'
            if dbsize > 450:
                msg += '\n🚨 <b>DB perto da quota (500MB)!</b>'
        except Exception as e:
            log_line(f'housekeeping falhou: {e}')
        runway = stock_runway(H)
        msg += f'\n📦 Estoque amostrado (8 nichos): {runway} leads'
        if runway < 120:
            # reposição automática via Tavily (cap 120, dedupe contra CRM)
            try:
                import replenish_leads
                rep = replenish_leads.run(cap=120)
                got = sum(rep.values())
                msg += f'\n🔄 Reposição automática Tavily: +{got} leads novos ({", ".join(f"{k}+{v}" for k, v in rep.items() if v)})'
            except Exception as e:
                log_line(f'replenish falhou: {e}')
                msg += '\n⚠️ <b>ESTOQUE BAIXO</b> e reposição automática FALHOU — verificar replenish_leads.py'
    log_line(msg.replace('\n', ' | '))
    if not DRY: tg_send(msg)
    log_line('===== FIM =====')


if __name__ == '__main__':
    main()
