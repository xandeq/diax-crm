# -*- coding: utf-8 -*-
"""Testes offline (sem rede/DB) das funções puras do orquestrador diário."""
import datetime as dt, sys, os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from waves_lib import compute_wave_slots, fup_candidates, brt_label

D = dt.date(2026, 9, 3)
def at(h, m=0): return dt.datetime(2026, 9, 3, h, m)

# --- compute_wave_slots -------------------------------------------------
def test_on_time_run_keeps_default_slots():
    # task 08:40 BRT = 11:40 UTC -> 09:00 / 10:30 / 11:30 BRT
    assert compute_wave_slots(at(11, 40)) == [at(12, 0), at(13, 30), at(14, 30)]

def test_late_run_shifts_past_slots_forward_and_keeps_gap():
    # incidente 03/09: task rodou 13:13 BRT (16:13 UTC) -> todos os slots no passado
    slots = compute_wave_slots(at(16, 13))
    assert slots == [at(16, 23), at(17, 23), at(18, 23)]

def test_partially_late_run_only_shifts_past_slots():
    # 12:30 UTC: 1a onda passou -> 12:40; gap de 60min empurra 2a p/ 13:40 e 3a p/ 14:40
    assert compute_wave_slots(at(12, 30)) == [at(12, 40), at(13, 40), at(14, 40)]

def test_gap_is_enforced_when_shift_collides_with_next_slot():
    # 13:00 UTC: 1a -> 13:10; 2a slot 13:30 < 13:10+60min -> 14:10; 3a 14:30 < 15:10 -> 15:10
    assert compute_wave_slots(at(13, 0)) == [at(13, 10), at(14, 10), at(15, 10)]

def test_too_late_returns_empty():
    # 17:05 BRT = 20:05 UTC > cutoff 17:00 BRT -> nada hoje
    assert compute_wave_slots(at(20, 5)) == []

def test_cutoff_truncates_trailing_waves():
    # 16:20 BRT = 19:20 UTC: 1a 19:30 ok, 2a 20:30 > cutoff -> só 1 onda
    assert compute_wave_slots(at(19, 20)) == [at(19, 30)]

def test_brt_label():
    assert brt_label(at(12, 0)) == '09:00'
    assert brt_label(at(16, 23)) == '13:23'

# --- fup_candidates -----------------------------------------------------
def test_fup_candidates_window_and_exclusions():
    sent = {'a@x.br': '2026-08-27',   # 7d  -> dentro
            'b@x.br': '2026-08-13',   # 21d -> dentro (limite)
            'c@x.br': '2026-08-12',   # 22d -> fora
            'd@x.br': '2026-09-01',   # 2d  -> cedo demais
            'e@x.br': '2026-08-20',   # dentro, mas já recebeu esta etapa
            'f@x.br': '2026-08-20',   # dentro, mas excluído (clicou/respondeu)
            'g@x.br': 'lixo'}         # data inválida -> ignorado
    out = fup_candidates(sent, done={'e@x.br': '2026-08-30'}, today=D, min_days=7, max_days=21, exclude={'f@x.br'})
    assert out == ['b@x.br', 'a@x.br']   # mais antigo primeiro

def test_fup_candidates_empty():
    assert fup_candidates({}, {}, D, 7, 21, set()) == []

# --- is_role_mailbox ----------------------------------------------------
def test_role_mailboxes_are_skipped_in_followups():
    from waves_lib import is_role_mailbox
    # vistos no dry-run de 03/09: caixas institucionais que nunca viram cliente
    for em in ('ouvidoria@clinicamediar.com', 'denuncia@agenciasabia.com.br', 'rh@clinicaidem.com.br',
               'publicidade@exame.com', 'faleconosco@agenciasus.org.br', 'nfe@empresa.com.br',
               'sac@loja.com.br', 'juridico@x.com.br', 'noreply@x.com.br', 'x@prefeitura.gov.br'):
        assert is_role_mailbox(em), em
    for em in ('contato@abreujudice.com.br', 'comercial@barretoeng.com', 'miqueias@embrasi.com.br',
               'reservas@vitoriapraia.com.br', 'atendimento@clinica.com.br'):
        assert not is_role_mailbox(em), em
