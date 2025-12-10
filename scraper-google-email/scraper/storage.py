from __future__ import annotations

import csv
import logging
import re
from datetime import datetime
from pathlib import Path
from typing import Iterable

from .extractor import EmailRecord
from .utils import LOGGER_NAME


def _slugify(query: str) -> str:
    """Converte a query em um slug simples, mantendo acentos."""
    cleaned = re.sub(r"\s+", "_", query.strip())
    cleaned = re.sub(r"[^\w\-]+", "_", cleaned, flags=re.UNICODE)
    cleaned = re.sub(r"_+", "_", cleaned)
    return cleaned.strip("_")


def _build_output_path(search_query: str, output_path: str) -> Path:
    """
    Gera um caminho de saída dinâmico no formato:
    <dir>/<slug>-<ddmmyyyy>.csv
    """
    base = Path(output_path)
    directory = base.parent if base.suffix else base
    directory.mkdir(parents=True, exist_ok=True)

    slug = _slugify(search_query) or "resultado"
    now = datetime.now()
    date_part = now.strftime("%d%m%Y")
    time_part = now.strftime("%H%M%S")
    filename = f"{slug}-{date_part}-{time_part}.csv"
    return directory / filename


def save_to_csv(records: Iterable[EmailRecord], search_query: str, output_path: str) -> None:
    """Garante pasta de saída e grava registros em CSV."""
    logger = logging.getLogger(LOGGER_NAME)
    path = _build_output_path(search_query, output_path)

    fieldnames = [
        "company_name",
        "email",
        "phone",
        "website",
        "source_url",
        "search_query",
        "timestamp",
    ]
    timestamp = datetime.utcnow().isoformat()
    file_exists = path.exists() and path.stat().st_size > 0

    with path.open(mode="a", newline="", encoding="utf-8") as csvfile:
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        if not file_exists:
            writer.writeheader()

        for record in records:
            writer.writerow(
                {
                    "company_name": record.company_name,
                    "email": record.email,
                    "phone": record.phone,
                    "website": record.website,
                    "source_url": record.source_url,
                    "search_query": search_query,
                    "timestamp": timestamp,
                }
            )

    logger.info("Registros salvos em %s", path)
