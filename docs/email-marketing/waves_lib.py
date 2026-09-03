# -*- coding: utf-8 -*-
"""Funções puras do orquestrador diário (daily_waves.py) — testáveis offline.

compute_wave_slots: horários das ondas tolerantes a atraso da task.
  Incidente 03/09/2026: PC dormindo às 08:40 -> task rodou 13:13 BRT
  (StartWhenAvailable) -> /schedule rejeitou horário no passado (400) ->
  11 campanhas ficaram Draft -> queue 400 -> 0 emails no dia.
fup_candidates: seleção de destinatários de uma etapa de follow-up a partir
  do state (email -> data ISO da etapa anterior).
"""
import datetime as dt

WAVE_SLOTS_UTC = [(12, 0), (13, 30), (14, 30)]   # 09:00 / 10:30 / 11:30 BRT
CUTOFF_UTC = (20, 0)                             # 17:00 BRT: depois disso, nada hoje
MIN_LEAD_MIN = 10                                # folga mínima p/ o worker pegar
GAP_MIN = 60                                     # respeita cadência/hora dos providers


def compute_wave_slots(now_utc, slots=WAVE_SLOTS_UTC, min_lead_min=MIN_LEAD_MIN,
                       gap_min=GAP_MIN, cutoff=CUTOFF_UTC):
    """Lista de datetimes UTC (naive) p/ cada onda. Slot no passado -> empurrado
    p/ now+min_lead; ondas seguintes mantêm gap mínimo; corta no cutoff."""
    day = now_utc.date()
    earliest = now_utc + dt.timedelta(minutes=min_lead_min)
    cutoff_dt = dt.datetime.combine(day, dt.time(*cutoff))
    out, prev = [], None
    for h, m in slots:
        s = max(dt.datetime.combine(day, dt.time(h, m)), earliest)
        if prev is not None:
            s = max(s, prev + dt.timedelta(minutes=gap_min))
        if s > cutoff_dt:
            break
        out.append(s); prev = s
    return out


def brt_label(when_utc):
    return (when_utc - dt.timedelta(hours=3)).strftime('%H:%M')


ROLE_LOCALPARTS = {
    'ouvidoria', 'denuncia', 'denuncias', 'rh', 'recrutamento', 'curriculo', 'curriculos', 'vagas',
    'publicidade', 'imprensa', 'press', 'faleconosco', 'sac', 'nfe', 'nf-e', 'nfse', 'fiscal',
    'financeiro', 'cobranca', 'juridico', 'legal', 'compliance', 'privacidade', 'dpo', 'lgpd',
    'noreply', 'no-reply', 'nao-responda', 'naoresponda', 'postmaster', 'abuse', 'webmaster',
    'suporte', 'support', 'helpdesk', 'ti', 'sistemas',
}
ROLE_DOMAIN_SUFFIXES = ('.gov.br', '.org.br', '.edu.br', '.jus.br', '.leg.br', '.mil.br')


def is_role_mailbox(email):
    """Caixa institucional/de função — nunca vira cliente, só queima reputação.
    Aplicado nos follow-ups (a wave inicial já saiu; não repetir o erro)."""
    em = (email or '').lower().strip()
    if '@' not in em: return True
    local, domain = em.rsplit('@', 1)
    if local in ROLE_LOCALPARTS or local.split('.')[0] in ROLE_LOCALPARTS: return True
    return domain.endswith(ROLE_DOMAIN_SUFFIXES)


def fup_candidates(sent, done, today, min_days, max_days, exclude):
    """sent: {email: 'YYYY-MM-DD' da etapa anterior}; done: {email: ...} desta etapa.
    Retorna emails com min_days <= idade <= max_days, não feitos, não excluídos,
    mais antigos primeiro (quem espera há mais tempo sai primeiro do cap)."""
    out = []
    for em, iso in sent.items():
        if em in done or em in exclude:
            continue
        try:
            age = (today - dt.date.fromisoformat(iso)).days
        except (TypeError, ValueError):
            continue
        if min_days <= age <= max_days:
            out.append((age, em))
    return [em for _, em in sorted(out, key=lambda t: (-t[0], t[1]))]
