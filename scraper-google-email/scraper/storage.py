from __future__ import annotations

import csv
import logging
import re
from datetime import datetime
from pathlib import Path
from typing import Iterable
from urllib.parse import urlparse

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


def _extract_domain_slug(site_url: str) -> str:
    """Extrai rótulo do domínio (entre www e o primeiro ponto)."""
    normalized = site_url if "://" in site_url else f"https://{site_url}"
    parsed = urlparse(normalized)
    host = (parsed.netloc or parsed.path or "").split("@")[-1]
    host = host.split(":")[0].lstrip(".")

    if host.startswith("www."):
        host = host[4:]

    if not host:
        return "site"

    primary = host.split(".")[0]
    slug = re.sub(r"[^\w\-]+", "_", primary, flags=re.UNICODE)
    slug = re.sub(r"_+", "_", slug).strip("_")
    return slug or "site"


def _build_site_output_path(site_url: str, output_path: str) -> Path:
    """
    Gera caminho de saída para modo site no formato:
    emails_<dominio>-<ddmmyyyy>-<hhmmss>.csv
    """
    base = Path(output_path)
    directory = base.parent if base.suffix else base
    directory.mkdir(parents=True, exist_ok=True)

    domain_slug = _extract_domain_slug(site_url)
    now = datetime.now()
    date_part = now.strftime("%d%m%Y")
    time_part = now.strftime("%H%M%S")
    filename = f"emails_{domain_slug}-{date_part}-{time_part}.csv"
    return directory / filename


def save_to_csv(records: Iterable[EmailRecord], search_query: str, output_path: str) -> None:
    """Garante pasta de saída e grava registros em CSV."""
    logger = logging.getLogger(LOGGER_NAME)
    path = _build_output_path(search_query, output_path)

    fieldnames = [
        "name",
        "email",
        "phone",
        "city",
        "state",
        "timestamp",
        "sent",
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
                    "name": record.company_name,
                    "email": record.email,
                    "phone": record.phone,
                    "city": record.city,
                    "state": record.state,
                    "timestamp": timestamp,
                    "sent": "",
                }
            )

    logger.info("Registros salvos em %s", path)


def save_site_emails(records: Iterable[EmailRecord], site_url: str, output_path: str) -> None:
    """Salva registros em arquivo com padrão emails_<dom>-data-hora.csv."""
    logger = logging.getLogger(LOGGER_NAME)
    path = _build_site_output_path(site_url, output_path)

    fieldnames = ["name", "email", "phone", "city", "state", "timestamp", "sent"]
    timestamp = datetime.utcnow().isoformat()
    file_exists = path.exists() and path.stat().st_size > 0

    with path.open(mode="a", newline="", encoding="utf-8") as csvfile:
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        if not file_exists:
            writer.writeheader()

        for record in records:
            writer.writerow({
                "name": record.company_name,
                "email": record.email,
                "phone": record.phone,
                "city": record.city,
                "state": record.state,
                "timestamp": timestamp,
                "sent": "",
            })

    logger.info("Registros salvos em %s", path)
