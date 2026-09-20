# -*- coding: utf-8 -*-
"""A/B DE SUBJECT — medição por taxa de clique (EMAL-03, Phase 12 plano 03).

A alternação por dia (SUBJ_B/ab_flip/sufixo sA-sB no nome da campanha) já existe em
daily_waves.py desde a Phase 10 e continua intocada por este módulo — ele só LÊ o
resultado agrupando as campanhas pelo sufixo que o motor já grava.

Vencedor é decidido por TAXA DE CLIQUE, nunca por open rate — decisão travada do
usuário (12-CONTEXT.md D-05): Apple MPP infla opens artificialmente, e o próprio
BrevoWebhookController.cs deste projeto já desconfia de open por esse motivo.
Open/bounce aparecem no relatório só como informativo.

MIN_CLICKS/MIN_DELTA abaixo são discricionários (CONTEXT.md deixou "onde o resultado
fica visível" a critério de implementação) — 30 cliques mínimos porque com a amostra
real medida (12 cliques/30d), qualquer limiar menor produziria veredito instável de
uma semana pra outra; 20% de diferença relativa porque abaixo disso o "vencedor" é
mais provavelmente ruído do que sinal, dado o volume atual de envio.

Uso:
  python ab_report.py                    # roda report() e imprime format_tg()
  python ab_report.py --days N           # janela em dias (default 30)
  python ab_report.py --json             # imprime o dict de report() como JSON
  python ab_report.py --write            # grava previews/ab-report.json
  python ab_report.py --selftest         # offline, cobre o bloco <behavior> do plano, exit 0/1
"""
import os
import sys
import re
import json
import pathlib
import datetime

DIR = str(pathlib.Path(__file__).resolve().parent) + '/'
if DIR not in sys.path:
    sys.path.insert(0, DIR)
sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')  # padrao daily_waves.py linha 20
import db as crmdb  # noqa: E402  (padrao daily_waves.py linha 24 — so define connect(), nao conecta no import)

MIN_CLICKS = 30      # abaixo disso nenhum vencedor eh defensavel -> INCONCLUSIVO (discricionario)
MIN_DELTA = 0.20      # diferenca relativa de taxa de clique abaixo disso -> EMPATE (discricionario)
DEFAULT_DAYS = 30
OUT = DIR + 'previews/ab-report.json'


def log_line(msg):
    """Log leve, stderr — este modulo eh so-leitura e nao precisa do log em arquivo de daily_waves.py."""
    print(f'[ab_report] {msg}', file=sys.stderr)


# ---------- funcoes puras (testaveis offline, sem DB — ver <behavior> do 12-03-PLAN.md) ----------

def parse_variant(name):
    """'AQ - loja - ... auto sB' -> 'sB'. Qualquer outro sufixo (ex.: 'auto wm', do 12-04) -> None
    DE PROPOSITO: webmail e outra populacao, misturar contaminaria as duas variantes."""
    m = re.search(r' auto (s[AB])$', name or '')
    return m.group(1) if m else None


def click_rate(d):
    sent = d.get('sent') or 0
    if sent <= 0:
        return 0.0
    return d.get('clicks', 0) / sent


def open_rate(d):
    """Informativo apenas — NUNCA usado para decidir vencedor (D-05: Apple MPP infla opens)."""
    sent = d.get('sent') or 0
    if sent <= 0:
        return 0.0
    return d.get('opens', 0) / sent


def veredito(stats, min_clicks=MIN_CLICKS, min_delta=MIN_DELTA):
    """stats = {'sA': {'sent','clicks',...}, 'sB': {...}} -> (veredito, motivo).
    Ordem exata: soma sent==0 -> SEM-DADOS; soma clicks < min_clicks -> INCONCLUSIVO;
    diferenca relativa de taxa de clique < min_delta -> EMPATE; senao, maior taxa de clique.
    Nunca referencia opens em nenhum ramo de decisao (D-05)."""
    a = stats.get('sA', {}) or {}
    b = stats.get('sB', {}) or {}
    sent_total = (a.get('sent') or 0) + (b.get('sent') or 0)
    if sent_total == 0:
        return 'SEM-DADOS', 'sem envios de sA/sB no periodo'

    clicks_total = (a.get('clicks') or 0) + (b.get('clicks') or 0)
    if clicks_total < min_clicks:
        return 'INCONCLUSIVO', f'{clicks_total} cliques no total < {min_clicks} minimo para decidir'

    rate_a, rate_b = click_rate(a), click_rate(b)
    maior = max(rate_a, rate_b)
    diff_rel = abs(rate_a - rate_b) / maior if maior > 0 else 0.0
    if diff_rel < min_delta:
        return ('EMPATE',
                f'diferenca relativa de taxa de clique {diff_rel*100:.1f}% < {min_delta*100:.0f}% minimo '
                f'(sA {rate_a*100:.2f}% vs sB {rate_b*100:.2f}%)')

    vencedor = 'sA' if rate_a > rate_b else 'sB'
    perdedor = 'sB' if vencedor == 'sA' else 'sA'
    rate_v = rate_a if vencedor == 'sA' else rate_b
    rate_p = rate_b if vencedor == 'sA' else rate_a
    return vencedor, f'{vencedor} taxa de clique {rate_v*100:.2f}% vs {perdedor} {rate_p*100:.2f}%'


# ---------- consulta (unica, agrupando por variante) ----------

def _empty_variant():
    return {'sent': 0, 'clicks': 0, 'opens': 0, 'bounces': 0, 'dias': 0, 'campanhas': 0}


def _query_stats(days):
    days = int(days)  # nunca concatenar string vinda de argumento sem int()
    cx = crmdb.connect()
    cur = cx.cursor()
    cur.execute("""
      SELECT RIGHT(c.name, 2) AS variante,
             COUNT(DISTINCT q.id) AS sent,
             COUNT(DISTINCT CASE WHEN e.event_type=3 THEN e.queue_item_id END) AS clicks,
             COUNT(DISTINCT CASE WHEN e.event_type=2 THEN e.queue_item_id END) AS opens,
             COUNT(DISTINCT CASE WHEN e.event_type=4 THEN e.queue_item_id END) AS bounces,
             COUNT(DISTINCT CONVERT(date, q.sent_at)) AS dias,
             COUNT(DISTINCT c.id) AS campanhas
      FROM email_queue_items q
      JOIN email_campaigns c ON c.id = q.campaign_id
      LEFT JOIN email_events e ON e.queue_item_id = q.id
      WHERE q.status = 2
        AND q.sent_at >= DATEADD(day, -?, SYSUTCDATETIME())
        AND c.name LIKE 'AQ - %'
        AND c.name LIKE '% auto s[AB]'
      GROUP BY RIGHT(c.name, 2)
    """, days)
    rows = cur.fetchall()
    cx.close()

    out = {'sA': _empty_variant(), 'sB': _empty_variant()}
    for r in rows:
        variante = r.variante
        if variante in out:
            out[variante] = {'sent': r.sent or 0, 'clicks': r.clicks or 0, 'opens': r.opens or 0,
                              'bounces': r.bounces or 0, 'dias': r.dias or 0, 'campanhas': r.campanhas or 0}
    return out


def report(days=DEFAULT_DAYS):
    """dict {'janela_dias','gerado_em','variantes':{'sA','sB'},'veredito','motivo','subject_exemplos'}.
    sA e sB sempre existem no dict (zerados se a query nao devolveu a linha)."""
    variantes = _query_stats(days)
    v, motivo = veredito(variantes)

    subject_exemplos = {}
    try:
        # import preguicoso: puxar daily_waves no topo do modulo traria reply_watch/site_check junto
        # e encareceria o smoke (waves_smoke.py importa TODO .py de docs/email-marketing/).
        import daily_waves
        for nicho in list(daily_waves.SHORT_SUBJ)[:3]:
            subject_exemplos[nicho] = (daily_waves.SHORT_SUBJ.get(nicho), daily_waves.SUBJ_B.get(nicho))
    except Exception as e:
        log_line(f'subject_exemplos: import daily_waves falhou ({e}) — seguindo com subject_exemplos vazio')

    return {
        'janela_dias': int(days),
        'gerado_em': datetime.datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%SZ'),
        'variantes': variantes,
        'veredito': v,
        'motivo': motivo,
        'subject_exemplos': subject_exemplos,
    }


def format_tg(rep):
    """String HTML estilo weekly_report() (emoji + <b>), pronta pro resumo de sexta."""
    a = rep['variantes'].get('sA', {}) or {}
    b = rep['variantes'].get('sB', {}) or {}

    def linha(nome, d):
        rate = click_rate(d) * 100
        return (f"  {nome}: {d.get('clicks', 0)}/{d.get('sent', 0)} cliques ({rate:.2f}%) · "
                f"{d.get('dias', 0)}d · {d.get('campanhas', 0)} camp.")

    return (f"🅰️🅱️ <b>A/B de subject ({rep['janela_dias']}d)</b>\n"
            f"{linha('sA', a)}\n{linha('sB', b)}\n"
            f"  Veredito: <b>{rep['veredito']}</b> — {rep['motivo']}\n"
            f"  (opens informativos, não decidem — Apple MPP)")


# ---------- selftest offline (cobre TODAS as assertivas do bloco <behavior> do plano) ----------

def _selftest():
    checks = [
        ("veredito INCONCLUSIVO (25 cliques < 30)",
         lambda: veredito({'sA': {'sent': 400, 'clicks': 20}, 'sB': {'sent': 400, 'clicks': 5}}, min_clicks=30)[0] == 'INCONCLUSIVO'),
        ("veredito sA vence (40 cliques >= 30; 7.5% vs 2.5%)",
         lambda: veredito({'sA': {'sent': 400, 'clicks': 30}, 'sB': {'sent': 400, 'clicks': 10}}, min_clicks=30)[0] == 'sA'),
        ("veredito EMPATE (diferenca relativa < 20%)",
         lambda: veredito({'sA': {'sent': 400, 'clicks': 20}, 'sB': {'sent': 400, 'clicks': 21}}, min_clicks=30)[0] == 'EMPATE'),
        ("veredito SEM-DADOS (sent=0 nas duas variantes)",
         lambda: veredito({'sA': {'sent': 0, 'clicks': 0}, 'sB': {'sent': 0, 'clicks': 0}})[0] == 'SEM-DADOS'),
        ("click_rate(400 sent, 20 clicks) == 0.05",
         lambda: click_rate({'sent': 400, 'clicks': 20}) == 0.05),
        ("click_rate(sent=0) == 0.0 sem ZeroDivisionError",
         lambda: click_rate({'sent': 0, 'clicks': 0}) == 0.0),
        ("parse_variant reconhece sufixo 'auto sB'",
         lambda: parse_variant('AQ - loja - Loja Online - 2026-09-18 auto sB') == 'sB'),
        ("parse_variant devolve None para campanha webmail ('auto wm')",
         lambda: parse_variant('AQ - webmail - Presenca Digital - 2026-09-22 auto wm') is None),
        ("parse_variant devolve None para campanha legada sem sufixo",
         lambda: parse_variant('AQ - loja - Loja Online - 2026-06-01') is None),
    ]
    ok = True
    for desc, fn in checks:
        try:
            passed = bool(fn())
        except Exception as e:
            passed = False
            desc = f'{desc} (EXCEPTION: {e})'
        print(f"{'OK' if passed else 'FAIL'}: {desc}")
        ok = ok and passed
    return ok


# ---------- CLI ----------

def main():
    args = sys.argv[1:]
    if '--selftest' in args:
        sys.exit(0 if _selftest() else 1)

    days = DEFAULT_DAYS
    if '--days' in args:
        i = args.index('--days')
        days = int(args[i + 1])

    rep = report(days=days)

    if '--write' in args:
        os.makedirs(os.path.dirname(OUT), exist_ok=True)
        with open(OUT, 'w', encoding='utf-8') as f:
            json.dump(rep, f, ensure_ascii=False, indent=2)
        log_line(f'gravado {OUT}')

    if '--json' in args:
        print(json.dumps(rep, ensure_ascii=False, indent=2))
    else:
        print(format_tg(rep))


if __name__ == '__main__':
    main()
