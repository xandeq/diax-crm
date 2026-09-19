"""
backfill_lead_scores.py — Gatilho determinístico do backfill de score do backlog (BFIL-01).

Dispara o motor OFICIAL em C# (LeadScoringService.RecomputeAllAsync) via
POST /api/v1/pipeline/recompute-scores.
NUNCA chamar POST /api/v1/outreach/segment — é o motor legado 0-6 pontos do
OutreachService, formula diferente.
Nenhum calculo de score acontece neste arquivo.

Este script é SOMENTE de orquestração HTTP: autentica, opcionalmente dispara o
endpoint oficial de recompute e captura snapshots de evidência antes/depois via
GET /api/v1/sales-dashboard e GET /api/v1/leads. Não reimplementa nenhuma
fórmula de score/segmento — essa é exatamente a lacuna que a BFIL-01 existe
para fechar (o script anterior, `outreach_priority_valid_leads.csv`, era um fit
score improvisado em Python).

Uso:
    python backfill_lead_scores.py --snapshot-only          # só captura, zero escrita
    python backfill_lead_scores.py --run                    # backfill de verdade
    python backfill_lead_scores.py --run --idempotency       # + prova de idempotência
    python backfill_lead_scores.py --snapshot-only --out PATH
"""

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

import requests

sys.path.insert(0, str(Path(__file__).parent))
from fetch_and_classify_leads import load_secrets, login  # noqa: E402

DEFAULT_API_BASE = "https://api.alexandrequeiroz.com.br"
RECOMPUTE_PATH = "/api/v1/pipeline/recompute-scores"
DASHBOARD_PATH = "/api/v1/sales-dashboard"
LEADS_PATH = "/api/v1/leads"
OPEN_STAGES = ("Lead", "Contacted", "Qualified", "Negotiating")
SEGMENTS = ("Hot", "Warm", "Cold")
RECOMPUTE_TIMEOUT = 300  # Pitfall 4: backfill de ~5k linhas em hospedagem compartilhada
DEFAULT_OUT = Path(__file__).parents[1] / "docs" / "lead-quality"


def capture_snapshot(api_base: str, token: str, label: str) -> dict:
    headers = {"Authorization": f"Bearer {token}"}

    dash_resp = requests.get(f"{api_base}{DASHBOARD_PATH}", headers=headers, timeout=60)
    dash_resp.raise_for_status()
    dash = dash_resp.json()

    open_total = sum(
        stage.get("count", 0)
        for stage in dash.get("funnel", [])
        if stage.get("stage") in OPEN_STAGES
    )
    hot = dash.get("hotLeads", 0)
    warm = dash.get("warmLeads", 0)
    cold = dash.get("coldLeads", 0)

    segment_totals = {}
    samples = {}
    for seg in SEGMENTS:
        leads_resp = requests.get(
            f"{api_base}{LEADS_PATH}",
            headers=headers,
            params={"segment": seg, "page": 1, "pageSize": 10},
            timeout=60,
        )
        leads_resp.raise_for_status()
        leads_data = leads_resp.json()
        items = leads_data.get("items", [])
        segment_totals[seg] = leads_data.get("totalCount", 0)
        samples[seg] = [
            {
                "id": item.get("id"),
                "name": item.get("name"),
                "email": item.get("email"),
                "leadScore": item.get("leadScore"),
                "segment": item.get("segment"),
                "status": item.get("status"),
                "source": item.get("source"),
                "createdAt": item.get("createdAt"),
            }
            for item in items
        ]

    return {
        "label": label,
        "capturedAt": datetime.now(timezone.utc).isoformat(),
        "funnel": dash["funnel"],
        "hotLeads": hot,
        "warmLeads": warm,
        "coldLeads": cold,
        "openLeadsTotal": open_total,
        "unsegmentedOpenLeads": open_total - (hot + warm + cold),
        "segmentTotals": segment_totals,
        "samples": samples,
    }


def trigger_recompute(api_base: str, token: str) -> dict:
    headers = {"Authorization": f"Bearer {token}"}
    try:
        r = requests.post(f"{api_base}{RECOMPUTE_PATH}", headers=headers, timeout=RECOMPUTE_TIMEOUT)
    except requests.exceptions.Timeout:
        print(
            "[aviso] HTTP estourou o timeout — a transacao no banco pode ter completado mesmo "
            "assim. NAO re-disparar cegamente: rode --snapshot-only e compare antes de decidir."
        )
        return {"ok": False, "timedOut": True}

    if r.status_code >= 400:
        return {"ok": False, "status": r.status_code, "body": r.text[:300]}

    return {"ok": True, "summary": r.json()}


def compare(a: dict, b: dict) -> list:
    keys = ("hotLeads", "warmLeads", "coldLeads", "openLeadsTotal", "unsegmentedOpenLeads")
    diffs = []
    for k in keys:
        if a.get(k) != b.get(k):
            diffs.append(f"{k}: {a.get(k)} -> {b.get(k)}")
    return diffs


def write_json(path: Path, obj: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)
    print(f"[out] {path}")


def print_snapshot_summary(snapshot: dict) -> None:
    print(f"\n=== Snapshot: {snapshot['label']} ({snapshot['capturedAt']}) ===")
    print("  Funil:")
    for stage in snapshot["funnel"]:
        print(f"    {stage.get('stage')}: {stage.get('count')}")
    print(f"  Hot={snapshot['hotLeads']}  Warm={snapshot['warmLeads']}  Cold={snapshot['coldLeads']}")
    print(f"  Leads abertos (Lead+Contacted+Qualified+Negotiating): {snapshot['openLeadsTotal']}")
    print(f"  unsegmentedOpenLeads (metrica-chave do backfill): {snapshot['unsegmentedOpenLeads']}")


def main():
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    ap = argparse.ArgumentParser(
        description="Dispara o backfill oficial de LeadScore/Segment (LeadScoringService) "
        "e captura evidencia antes/depois."
    )
    ap.add_argument("--snapshot-only", action="store_true",
                     help="Só autentica e captura snapshot, grava backfill_baseline.json. Zero escrita em produção.")
    ap.add_argument("--run", action="store_true",
                     help="Captura baseline, dispara o recompute oficial, captura o resultado depois.")
    ap.add_argument("--idempotency", action="store_true",
                     help="Só válido com --run. Dispara um SEGUNDO recompute e prova idempotência.")
    ap.add_argument("--out", type=Path, default=DEFAULT_OUT, help="Diretório de saída dos JSONs de evidência.")
    args = ap.parse_args()

    if not args.snapshot_only and not args.run:
        ap.print_help()
        sys.exit(1)

    if args.idempotency and not args.run:
        print("[erro] --idempotency só é válido junto de --run")
        sys.exit(1)

    secrets = load_secrets()
    api_base = secrets.get("DIAX_API_BASE_URL", DEFAULT_API_BASE).rstrip("/")
    token = login(api_base, secrets.get("DIAX_ADMIN_EMAIL"), secrets.get("DIAX_ADMIN_PASSWORD"))

    if args.snapshot_only:
        baseline = capture_snapshot(api_base, token, "baseline")
        write_json(args.out / "backfill_baseline.json", baseline)
        print_snapshot_summary(baseline)
        return

    # --run
    baseline = capture_snapshot(api_base, token, "baseline")
    write_json(args.out / "backfill_baseline.json", baseline)
    print_snapshot_summary(baseline)

    print("\n[trigger] disparando POST /api/v1/pipeline/recompute-scores...")
    result = trigger_recompute(api_base, token)
    if not result.get("ok"):
        print(f"[erro] recompute falhou: {result}")
        sys.exit(1)
    summary = result["summary"]
    print(
        f"[trigger] OK — leadsScored={summary.get('leadsScored')} hot={summary.get('hot')} "
        f"warm={summary.get('warm')} cold={summary.get('cold')} "
        f"followUpTasksCreated={summary.get('followUpTasksCreated')}"
    )

    after = capture_snapshot(api_base, token, "after")
    write_json(args.out / "backfill_after.json", after)
    print_snapshot_summary(after)

    diffs = compare(baseline, after)
    print("\n=== Diferenças baseline -> after ===")
    if diffs:
        for line in diffs:
            print(f"  {line}")
    else:
        print("  (nenhuma diferença)")

    if args.idempotency:
        print("\n[idempotency] disparando um SEGUNDO recompute...")
        result2 = trigger_recompute(api_base, token)
        if not result2.get("ok"):
            print(f"[erro] segundo recompute falhou: {result2}")
            sys.exit(1)
        summary2 = result2["summary"]
        print(
            f"[idempotency] OK — leadsScored={summary2.get('leadsScored')} "
            f"followUpTasksCreated={summary2.get('followUpTasksCreated')}"
        )

        after_rerun = capture_snapshot(api_base, token, "after_rerun")
        write_json(args.out / "backfill_after_rerun.json", after_rerun)
        print_snapshot_summary(after_rerun)

        rerun_diffs = compare(after, after_rerun)
        print("\n=== Diferenças after -> after_rerun (deve ser vazio) ===")
        if rerun_diffs:
            for line in rerun_diffs:
                print(f"  {line}")
            print("[erro] backfill NÃO é idempotente — ver diferenças acima")
            sys.exit(2)
        else:
            print("  (nenhuma diferença — idempotência confirmada)")


if __name__ == "__main__":
    main()
