from __future__ import annotations

import logging
import re
from dataclasses import dataclass
from typing import List
from urllib.parse import urlparse

from bs4 import BeautifulSoup

from .crawler import PageContent
from .utils import LOGGER_NAME, normalize_text

EMAIL_REGEX = re.compile(r"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")
PHONE_REGEX = re.compile(r"\+?\d[\d\s().-]{8,}")
IMAGE_EXTENSIONS = {"jpg", "jpeg", "png", "gif", "webp", "svg", "bmp", "tif", "tiff", "ico"}

# Estados brasileiros para extração
BRAZILIAN_STATES = {
    "AC": "Acre", "AL": "Alagoas", "AP": "Amapá", "AM": "Amazonas",
    "BA": "Bahia", "CE": "Ceará", "DF": "Distrito Federal", "ES": "Espírito Santo",
    "GO": "Goiás", "MA": "Maranhão", "MT": "Mato Grosso", "MS": "Mato Grosso do Sul",
    "MG": "Minas Gerais", "PA": "Pará", "PB": "Paraíba", "PR": "Paraná",
    "PE": "Pernambuco", "PI": "Piauí", "RJ": "Rio de Janeiro", "RN": "Rio Grande do Norte",
    "RS": "Rio Grande do Sul", "RO": "Rondônia", "RR": "Roraima", "SC": "Santa Catarina",
    "SP": "São Paulo", "SE": "Sergipe", "TO": "Tocantins"
}

# Regex para capturar padrões de cidade/estado
CITY_STATE_REGEX = re.compile(
    r"([A-ZÀ-Ú][a-zà-ú]+(?:\s+[A-ZÀ-Ú][a-zà-ú]+)*)[\s,\-]+([A-Z]{2})\b",
    re.UNICODE
)
PLACEHOLDER_DOMAINS = {"example.com", "test.com", "invalid.com", "localhost"}
COMMON_TLDS = {
    "com",
    "net",
    "org",
    "edu",
    "gov",
    "mil",
    "int",
    "info",
    "biz",
    "co",
    "io",
    "ai",
    "app",
    "dev",
    "me",
    "tv",
    "fm",
    "news",
    "store",
    "shop",
    "site",
    "online",
    "tech",
    "xyz",
    "club",
    "law",
    "legal",
    "adv",
    "adv.br",
    "br",
    "com.br",
    "net.br",
    "org.br",
    "art.br",
    "eng.br",
    "eco.br",
    "rec.br",
}


@dataclass
class EmailRecord:
    """Representa um contato encontrado em uma página."""

    company_name: str
    email: str
    phone: str
    city: str
    state: str
    website: str
    source_url: str


def _extract_company_name(soup: BeautifulSoup, source_url: str) -> str:
    """Extrai nome da empresa com heurísticas decrescentes."""
    meta = soup.find("meta", attrs={"property": "og:site_name"})
    if meta and meta.get("content"):
        return normalize_text(meta["content"])

    if soup.title and soup.title.string:
        return normalize_text(soup.title.string)

    h1 = soup.find("h1")
    if h1 and h1.get_text():
        return normalize_text(h1.get_text())

    parsed = urlparse(source_url)
    return parsed.netloc or source_url


def _extract_emails(html: str) -> List[str]:
    """Localiza emails via regex, valida e remove duplicados."""
    matches = EMAIL_REGEX.findall(html)
    unique = []
    seen = set()
    for match in matches:
        email = match.strip()
        if not _is_valid_email(email):
            continue
        if email.lower() in seen:
            continue
        seen.add(email.lower())
        unique.append(email)
    return unique


def _is_valid_email(email: str) -> bool:
    """Filtra emails óbvios inválidos/placeholders/ativos de mídia."""
    lower = email.lower()

    # Placeholder genérico
    if "example@" in lower or lower.endswith("@example.com"):
        return False

    # Remove se parece asset (imagem/video) pela TLD ou extensão
    if any(ext in lower for ext in (".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp", ".tif", ".tiff", ".ico", ".mp4", ".webm")):
        return False

    local, _, domain = lower.partition("@")
    if not local or not domain:
        return False

    # Domain placeholder
    if domain in PLACEHOLDER_DOMAINS:
        return False

    # Checa TLD
    if "." not in domain:
        return False
    *_, tld = domain.rsplit(".", 1)
    if not tld.isalpha() or not (2 <= len(tld) <= 10):
        return False
    if len(COMMON_TLDS) and tld not in COMMON_TLDS and domain not in COMMON_TLDS:
        # se não estiver na lista comum, ainda aceita se não for claramente asset
        pass

    # Evita domínios com espaços ou barras
    if " " in domain or "/" in domain:
        return False

    return True


def _extract_phone(html: str) -> str:
    """Retorna o primeiro telefone encontrado (se houver)."""
    matches = PHONE_REGEX.findall(html)
    if not matches:
        return ""
    # Normaliza espaços múltiplos
    return normalize_text(matches[0])


def _extract_city_state(html: str) -> tuple[str, str]:
    """Extrai cidade e estado do HTML usando padrões comuns."""
    matches = CITY_STATE_REGEX.findall(html)
    for city_candidate, state_abbr in matches:
        state_upper = state_abbr.upper()
        if state_upper in BRAZILIAN_STATES:
            return normalize_text(city_candidate), state_upper
    return "", ""


def extract_emails_and_company(page: PageContent) -> List[EmailRecord]:
    """Extrai emails e associa ao nome da empresa para uma página."""
    logger = logging.getLogger(LOGGER_NAME)

    if not page.html:
        logger.debug("Página vazia para %s", page.url)
        return []

    soup = BeautifulSoup(page.html, "lxml")
    company_name = _extract_company_name(soup, page.url)
    emails = _extract_emails(page.html)
    phone = _extract_phone(page.html)
    city, state = _extract_city_state(page.html)
    website = page.url

    logger.debug("Página %s: %d emails encontrados", page.url, len(emails))

    return [
        EmailRecord(
            company_name=company_name,
            email=email,
            phone=phone,
            city=city,
            state=state,
            website=website,
            source_url=page.url,
        )
        for email in emails
    ]
