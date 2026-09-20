# -*- coding: utf-8 -*-
"""Smoke de regressao do motor de cadencia (SCRP-01, Phase 10).

Offline, sem rede, < 5s. Prova que a extracao dos scripts fire_* para waves_core/waves_copy
nao mudou NADA do comportamento observavel de daily_waves.py. Falha em silencio e o risco desta
fase (COPY_BANK menor, import quebrado num dependente vivo) — este script a torna barulhenta.

Uso:
  python waves_smoke.py                    # roda os 6 checks, exit 0/1
  python waves_smoke.py --update-baseline  # regrava waves-baseline.json (so ANTES do refactor)
"""
import os
import sys
import ast
import json
import hashlib
import importlib
import pathlib
import datetime

DIR = str(pathlib.Path(__file__).resolve().parent)
if DIR not in sys.path:
    sys.path.insert(0, DIR)

BASELINE_PATH = os.path.join(DIR, 'waves-baseline.json')

ARGS_TEMPLATE = dict(
    hook='Hook fixo de smoke',
    subhead='Subhead fixo de smoke',
    img_main='https://exemplo.invalid/img.jpg',
    intro='Intro fixa de smoke para congelar o template v2.',
    bullets=['Bullet A', 'Bullet B', 'Bullet C', 'Bullet D'],
    wa='Ola, mensagem fixa de smoke')

CHAVES_CFG = ('seg', 'search_terms', 'service', 'subject', 'hook', 'subhead', 'intro', 'bullets', 'wa', 'img')

# Arquivos que NUNCA devem ser importados por este smoke (efeito colateral no import).
# Fonte historica do padrao: ate o Phase 10 Plan 03, debug_campaign.py fazia um alias de
# `fire_today_20` (prefixo `fire_`, cai no filtro abaixo, nunca passado para
# importlib.import_module por este script). Religado no 10-03 para um alias de `waves_core`;
# esse modulo nao tem efeito colateral no import, entao segue seguro.
EXCLUDE_PREFIXES = ('fire_',)
EXCLUDE_NAMES = {'waves_smoke.py'}
EXCLUDE_DIRS = {'archive', '__pycache__'}


def _sha(obj):
    return hashlib.sha256(json.dumps(obj, sort_keys=True, ensure_ascii=False,
                                      default=str).encode('utf-8')).hexdigest()


def _load_baseline():
    if not os.path.exists(BASELINE_PATH):
        return None
    with open(BASELINE_PATH, 'r', encoding='utf-8') as f:
        return json.load(f)


def check_rotation_coberta(baseline):
    import daily_waves
    if len(daily_waves.ROTATION) != 20:
        return False, f'ROTATION tem {len(daily_waves.ROTATION)} nichos, esperado 20'
    faltando = set(daily_waves.ROTATION) - set(daily_waves.COPY_BANK)
    if faltando:
        return False, f'nichos da ROTATION ausentes em COPY_BANK: {sorted(faltando)}'
    if len(daily_waves.COPY_BANK) != 20:
        return False, f'COPY_BANK tem {len(daily_waves.COPY_BANK)} nichos, esperado 20'
    return True, ''


def check_copy_bank_integro(baseline):
    import daily_waves
    for nicho in daily_waves.ROTATION:
        cfg = daily_waves.COPY_BANK.get(nicho)
        if cfg is None:
            return False, f'nicho {nicho!r} ausente em COPY_BANK'
        for chave in CHAVES_CFG:
            if not cfg.get(chave):
                return False, f'nicho {nicho!r} tem chave {chave!r} vazia/ausente'
        bullets = cfg.get('bullets')
        if not isinstance(bullets, list) or len(bullets) < 3:
            return False, f'nicho {nicho!r} tem bullets invalidos (esperado lista com >= 3 itens): {bullets!r}'

    sha = _sha(daily_waves.COPY_BANK)
    if baseline is None:
        return False, 'baseline ausente'
    esperado = baseline.get('copy_bank_sha256')
    if sha != esperado:
        return False, f'sha256 de COPY_BANK diverge do baseline (atual={sha}, baseline={esperado})'
    return True, ''


def check_template_v2_identico(baseline):
    import daily_waves
    html = daily_waves.build_html_v2(**ARGS_TEMPLATE)
    sha = hashlib.sha256(html.encode('utf-8')).hexdigest()
    if baseline is None:
        return False, 'baseline ausente'
    esperado = baseline.get('template_v2_sha256')
    if sha != esperado:
        return False, f'sha256 de build_html_v2(...) diverge do baseline (atual={sha}, baseline={esperado})'
    return True, ''


def check_healthy_mutavel_in_place(baseline):
    import daily_waves
    src = sys.modules[daily_waves.assign_providers.__module__]
    orig = list(src.HEALTHY)
    try:
        src.HEALTHY[:] = ['ZZ1', 'ZZ2']
        if list(daily_waves.HEALTHY) != ['ZZ1', 'ZZ2']:
            return False, f'daily_waves.HEALTHY nao refletiu a mutacao in-place: {daily_waves.HEALTHY!r}'
        resultado = [ld['assignedProvider'] for ld in daily_waves.assign_providers([{}, {}, {}])]
        esperado = ['ZZ1', 'ZZ2', 'ZZ1']
        if resultado != esperado:
            return False, f'assign_providers nao ciclou corretamente: {resultado!r} != {esperado!r}'
    finally:
        src.HEALTHY[:] = orig
    return True, ''


def _module_name_for_import_node(node, local_modules):
    """Retorna o nome do modulo local (sem .py) referenciado por um Import/ImportFrom, ou None."""
    if isinstance(node, ast.ImportFrom):
        if node.level and node.level > 0:
            return None  # import relativo — fora de escopo aqui (nao usado nesta arvore)
        mod = node.module
        if mod and mod.split('.')[0] in local_modules:
            return mod.split('.')[0]
        return None
    if isinstance(node, ast.Import):
        return None  # tratado separadamente (pode ter varios nomes)
    return None


def check_imports_locais_resolvem(baseline):
    local_modules = set()
    for entry in os.listdir(DIR):
        if entry.endswith('.py'):
            local_modules.add(entry[:-3])

    candidatos = []
    for entry in sorted(os.listdir(DIR)):
        full = os.path.join(DIR, entry)
        if not os.path.isfile(full) or not entry.endswith('.py'):
            continue
        if entry in EXCLUDE_NAMES:
            continue
        if any(entry.startswith(p) for p in EXCLUDE_PREFIXES):
            continue
        candidatos.append(entry)

    checados = set()
    for entry in candidatos:
        full = os.path.join(DIR, entry)
        try:
            with open(full, 'r', encoding='utf-8') as f:
                tree = ast.parse(f.read(), filename=entry)
        except SyntaxError as e:
            return False, f'{entry}: erro de sintaxe ao parsear AST: {e}'

        for node in ast.walk(tree):
            if isinstance(node, ast.ImportFrom):
                if node.level and node.level > 0:
                    continue
                mod = node.module
                if not mod:
                    continue
                base_mod = mod.split('.')[0]
                if base_mod not in local_modules:
                    continue
                if base_mod in EXCLUDE_NAMES or any(base_mod.startswith(p) for p in EXCLUDE_PREFIXES):
                    continue
                nomes = [alias.name for alias in node.names if alias.name != '*']
                if base_mod not in checados:
                    try:
                        target_mod = importlib.import_module(base_mod)
                    except Exception as e:
                        return False, f'{entry}: nao foi possivel importar modulo local {base_mod!r} referenciado: {e}'
                    checados.add(base_mod)
                else:
                    target_mod = sys.modules.get(base_mod)
                for nome in nomes:
                    if not hasattr(target_mod, nome):
                        return False, f'{entry}: import de {base_mod!r} nao encontra nome {nome!r}'
            elif isinstance(node, ast.Import):
                for alias in node.names:
                    base_mod = alias.name.split('.')[0]
                    if base_mod not in local_modules:
                        continue
                    if base_mod in EXCLUDE_NAMES or any(base_mod.startswith(p) for p in EXCLUDE_PREFIXES):
                        continue
                    if base_mod not in checados:
                        try:
                            importlib.import_module(base_mod)
                        except Exception as e:
                            return False, f'{entry}: nao foi possivel importar modulo local {base_mod!r} referenciado: {e}'
                        checados.add(base_mod)
    return True, ''


def check_warmup_curva(baseline):
    """Offline puro — importa waves_core (nao daily_waves), nao le nada do baseline."""
    import waves_core as w

    if w.WARMUP_TARGETS != [87, 104, 125, 150, 180, 216, 260, 300]:
        return False, f'WARMUP_TARGETS inesperado: {w.WARMUP_TARGETS!r}'

    if w.warmup_target(1) != 87:
        return False, f'warmup_target(1) != 87: {w.warmup_target(1)!r}'
    if w.warmup_target(8) != 300:
        return False, f'warmup_target(8) != 300: {w.warmup_target(8)!r}'
    if w.warmup_target(99) != 300:
        return False, f'warmup_target(99) != 300 (sem clamp no teto): {w.warmup_target(99)!r}'
    if w.warmup_target(0) != 87:
        return False, f'warmup_target(0) != 87 (sem clamp no piso): {w.warmup_target(0)!r}'
    if w.warmup_target(-5) != 87:
        return False, f'warmup_target(-5) != 87 (sem clamp no piso): {w.warmup_target(-5)!r}'

    if w.max_per_seg(300, n_seg=12, n_prov=4) != 25:
        return False, f'max_per_seg(300,12,4) != 25: {w.max_per_seg(300, n_seg=12, n_prov=4)!r}'
    if w.max_per_seg(87, n_seg=12, n_prov=4) != 8:
        return False, f'max_per_seg(87,12,4) != 8: {w.max_per_seg(87, n_seg=12, n_prov=4)!r}'
    if w.max_per_seg(300, n_seg=12, n_prov=2) != 20:
        return False, f'max_per_seg(300,12,2) != 20 (clamp por provider): {w.max_per_seg(300, n_seg=12, n_prov=2)!r}'
    if w.max_per_seg(1, n_seg=12, n_prov=4) < 4:
        return False, f'max_per_seg nunca pode devolver menos que MIN_SEG(4): {w.max_per_seg(1, n_seg=12, n_prov=4)!r}'

    if w.next_warmup_day(3, 150, 125) != 4:
        return False, f'next_warmup_day(3,150,125) != 4 (>=80% deveria avancar): {w.next_warmup_day(3, 150, 125)!r}'
    if w.next_warmup_day(3, 40, 125) != 3:
        return False, f'next_warmup_day(3,40,125) != 3 (<80% deveria segurar): {w.next_warmup_day(3, 40, 125)!r}'
    if w.next_warmup_day(8, 300, 300) != 8:
        return False, f'next_warmup_day(8,300,300) != 8 (nao deveria passar do teto): {w.next_warmup_day(8, 300, 300)!r}'

    return True, ''


CHECKS = [
    ('rotation_coberta', check_rotation_coberta),
    ('copy_bank_integro', check_copy_bank_integro),
    ('template_v2_identico', check_template_v2_identico),
    ('healthy_mutavel_in_place', check_healthy_mutavel_in_place),
    ('imports_locais_resolvem', check_imports_locais_resolvem),
    ('warmup_curva', check_warmup_curva),
]


def _write_baseline():
    import daily_waves
    baseline = {
        'gerado_em': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'gerado_contra': 'pre-refactor (fire_today_20 + fire_w1_2707 + fire_w2w3_2707)',
        'rotation': list(daily_waves.ROTATION),
        'copy_bank_niches': sorted(daily_waves.COPY_BANK.keys()),
        'copy_bank_sha256': _sha(daily_waves.COPY_BANK),
        'template_v2_sha256': hashlib.sha256(
            daily_waves.build_html_v2(**ARGS_TEMPLATE).encode('utf-8')).hexdigest(),
        'healthy': ['Brevo', 'Mailjet', 'Resend', 'SendGrid'],
        'per_provider': 5,
    }
    with open(BASELINE_PATH, 'w', encoding='utf-8') as f:
        json.dump(baseline, f, ensure_ascii=False, indent=2, sort_keys=True)
        f.write('\n')
    print(f'baseline gravado em: {BASELINE_PATH}')
    return baseline


def main():
    if '--update-baseline' in sys.argv:
        _write_baseline()
        return 0

    baseline = _load_baseline()
    if baseline is None:
        print('FALHOU baseline: baseline ausente: rode `python waves_smoke.py --update-baseline` '
              'contra o codigo PRE-refactor')
        return 1

    falhas = 0
    for nome, fn in CHECKS:
        try:
            ok, motivo = fn(baseline)
        except Exception as e:
            ok, motivo = False, f'excecao: {e}'
        if ok:
            print(f'OK {nome}')
        else:
            print(f'FALHOU {nome}: {motivo}')
            falhas += 1

    total = len(CHECKS)
    print(f'== {total - falhas}/{total} OK ==')
    return 1 if falhas else 0


if __name__ == '__main__':
    sys.exit(main())
