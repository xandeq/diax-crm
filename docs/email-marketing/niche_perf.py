# -*- coding: utf-8 -*-
"""Kill/scale por nicho com dados (30 dias): quem não gera clique nem resposta depois de
N envios sai da rotação; quem gera, entra mais vezes.

rank_niches: puro (testável). load_stats: DB + state de respostas. Resultado vai p/
previews/niche-stats.json — daily_waves lê e pula nichos 'cut'; relatório de sexta lista.
"""
import json, os, re, datetime as dt

DIR = os.path.dirname(os.path.abspath(__file__)) + '/'
STATS_FILE = DIR + 'previews/niche-stats.json'
REPLIES_FILE = DIR + 'previews/replies-state.json'
CAMP_RE = re.compile(r'AQ - (\w+) - ')


def rank_niches(stats, min_sends_cut=150, min_sends_boost=40, top_n=3):
    """stats: {seg: {'sent','clicks','replies'}}.
    cut  = amostra suficiente e ZERO sinal (nem clique nem resposta).
    boost= melhores por sinal/envio (resposta vale 5 cliques), com amostra mínima."""
    cut, scored, table = [], [], []
    for seg, s in stats.items():
        sent = s.get('sent', 0) or 0; clicks = s.get('clicks', 0) or 0; replies = s.get('replies', 0) or 0
        score = (replies * 5 + clicks) / sent if sent else 0.0
        table.append((seg, sent, clicks, replies, score))
        if sent >= min_sends_cut and clicks == 0 and replies == 0:
            cut.append(seg)
        elif sent >= min_sends_boost and score > 0:
            scored.append((score, seg))
    boost = [seg for _, seg in sorted(scored, key=lambda t: -t[0])[:top_n]]
    table.sort(key=lambda t: -t[4])
    return {'cut': sorted(cut), 'boost': boost, 'table': table}


def format_niche_report(rank):
    lines = ['📊 <b>Nichos (30d)</b>']
    for seg, sent, clicks, replies, score in rank['table'][:12]:
        lines.append(f'  {seg:14} {sent:4} env · {clicks:2} clk · {replies:2} resp · {score*100:.1f}%')
    if rank['boost']: lines.append('  ⬆️ dobrar: ' + ', '.join(rank['boost']))
    if rank['cut']:   lines.append('  ✂️ cortar (0 sinal): ' + ', '.join(rank['cut']))
    return '\n'.join(lines)


def load_stats(days=30):
    """Envios (status=2) e cliques (event_type=3) por seg via nome da campanha; respostas
    do state do reply_watch (seg registrado na hora da resposta)."""
    import sys
    sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')
    import db as crmdb
    cx = crmdb.connect(); cur = cx.cursor()
    cur.execute("""
      SELECT c.name, COUNT(DISTINCT q.id) sent,
             COUNT(DISTINCT CASE WHEN e.event_type=3 THEN q.id END) clicks
      FROM email_queue_items q JOIN email_campaigns c ON c.id=q.campaign_id
      LEFT JOIN email_events e ON e.queue_item_id=q.id
      WHERE q.status=2 AND q.sent_at >= DATEADD(day,-?,SYSUTCDATETIME()) AND c.name LIKE 'AQ - %'
      GROUP BY c.name""", days)
    stats = {}
    for name, sent, clicks in cur.fetchall():
        m = CAMP_RE.match(name or '')
        if not m: continue
        s = stats.setdefault(m.group(1), {'sent': 0, 'clicks': 0, 'replies': 0})
        s['sent'] += sent; s['clicks'] += clicks
    cx.close()
    try:
        seen = json.load(open(REPLIES_FILE, encoding='utf-8')).get('seen', {})
        cutoff = (dt.date.today() - dt.timedelta(days=days)).isoformat()
        for em, info in seen.items():
            if info.get('at', '') >= cutoff and info.get('seg') in stats and info.get('intent') != 'negative':
                stats[info['seg']]['replies'] += 1
    except Exception:
        pass
    return stats


def refresh(days=30):
    rank = rank_niches(load_stats(days))
    os.makedirs(os.path.dirname(STATS_FILE), exist_ok=True)
    json.dump({'at': dt.date.today().isoformat(), 'days': days, **rank}, open(STATS_FILE, 'w', encoding='utf-8'),
              ensure_ascii=False, indent=1)
    return rank


def cut_list(max_age_days=10):
    """Nichos a pular hoje (arquivo recente); vazio se não há dados."""
    try:
        d = json.load(open(STATS_FILE, encoding='utf-8'))
        if (dt.date.today() - dt.date.fromisoformat(d['at'])).days > max_age_days:
            return []
        return list(d.get('cut', []))
    except Exception:
        return []


if __name__ == '__main__':
    print(format_niche_report(refresh()))
