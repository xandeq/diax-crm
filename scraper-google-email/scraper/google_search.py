from __future__ import annotations

import logging
import time
from typing import List, Optional
from urllib.parse import parse_qs, urlencode, urlparse

from selenium.common.exceptions import TimeoutException, WebDriverException
from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.support.ui import WebDriverWait

from config import GOOGLE_SEARCH_URL_BASE, REQUEST_DELAY_SECONDS_RANGE
from .utils import LOGGER_NAME, get_random_delay


def _build_search_url(query: str, page_index: int) -> str:
    """Monta URL de busca do Google com paginação por start."""
    start = (page_index - 1) * 10
    params = {"q": query, "start": start, "num": 10, "hl": "pt-BR"}
    return f"{GOOGLE_SEARCH_URL_BASE}?{urlencode(params)}"


def _resolve_google_redirect(href: str) -> Optional[str]:
    """Extrai URL real de links do tipo google.com/url?q=..."""
    parsed = urlparse(href)
    if "google." in parsed.netloc and parsed.path == "/url":
        qs = parse_qs(parsed.query)
        if "q" in qs and qs["q"]:
            return qs["q"][0]
    return href


def _accept_consent_if_present(driver) -> None:
    """Tenta aceitar o banner de cookies do Google, se aparecer."""
    try:
        WebDriverWait(driver, 5).until(
            EC.element_to_be_clickable(
                (
                    By.CSS_SELECTOR,
                    "#L2AGLb, button[aria-label*='Aceitar'], button[aria-label*='Accept']",
                )
            )
        ).click()
    except TimeoutException:
        return
    except WebDriverException:
        return


def _collect_results(driver) -> List[str]:
    """Coleta URLs orgânicas na página atual."""
    links: List[str] = []
    cards = driver.find_elements(By.CSS_SELECTOR, "div#search h3")
    for h3 in cards:
        try:
            anchor = h3.find_element(By.XPATH, "./ancestor::a[1]")
            href = anchor.get_attribute("href")
            if not href:
                continue
            resolved = _resolve_google_redirect(href)
            if resolved.startswith("http"):
                links.append(resolved)
        except WebDriverException:
            continue
    return links


def search_google(driver, query: str, max_pages: int) -> List[str]:
    """Percorre resultados orgânicos do Google e retorna URLs únicas."""
    logger = logging.getLogger(LOGGER_NAME)
    collected: List[str] = []

    for page in range(1, max_pages + 1):
        search_url = _build_search_url(query, page)
        logger.info("Carregando resultados do Google (página %d/%d)", page, max_pages)

        try:
            driver.get(search_url)
        except WebDriverException as exc:
            logger.error("Falha ao abrir o Google na página %d: %s", page, exc)
            break

        _accept_consent_if_present(driver)

        try:
            WebDriverWait(driver, 8).until(
                EC.presence_of_element_located((By.CSS_SELECTOR, "div#search"))
            )
        except TimeoutException:
            logger.warning("Timeout esperando resultados na página %d", page)

        results = _collect_results(driver)
        if not results:
            logger.warning("Nenhum resultado orgânico encontrado na página %d", page)
            break

        collected.extend(results)

        if page >= max_pages:
            break

        delay = get_random_delay(REQUEST_DELAY_SECONDS_RANGE)
        logger.debug("Aguardando %.2f segundos antes da próxima página.", delay)
        time.sleep(delay)

    unique_urls = list(dict.fromkeys(collected))
    logger.info("Total de URLs coletadas: %d", len(unique_urls))
    return unique_urls
