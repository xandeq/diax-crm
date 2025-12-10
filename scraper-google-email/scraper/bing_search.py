from __future__ import annotations

import logging
import time
from typing import List
from urllib.parse import urlencode

from selenium.common.exceptions import NoSuchElementException, TimeoutException, WebDriverException
from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.remote.webdriver import WebDriver

from config import BING_SEARCH_URL_BASE, REQUEST_DELAY_SECONDS_RANGE
from .utils import LOGGER_NAME, get_random_delay


def _build_search_url(query: str, page_index: int) -> str:
    """Monta URL de busca do Bing, paginando via parâmetro 'first'."""
    params = {"q": query}
    if page_index > 1:
        params["first"] = (page_index - 1) * 10 + 1
    return f"{BING_SEARCH_URL_BASE}?{urlencode(params)}"


def search_bing(driver: WebDriver, query: str, max_pages: int) -> List[str]:
    """Percorre páginas de resultado do Bing e coleta links orgânicos."""
    logger = logging.getLogger(LOGGER_NAME)
    collected: List[str] = []

    def _accept_consent_if_present() -> None:
        """Tenta aceitar o banner de consentimento do Bing se aparecer."""
        try:
            WebDriverWait(driver, 4).until(
                EC.element_to_be_clickable((By.CSS_SELECTOR, "#bnp_btn_accept, #bnp_ttc_optin, button[aria-label='Accept']"))
            ).click()
            logger.debug("Banner de cookies aceito.")
        except TimeoutException:
            pass
        except WebDriverException as exc:
            logger.debug("Não foi possível interagir com banner de cookies: %s", exc)

    def _find_results() -> List[str]:
        """Busca resultados orgânicos usando seletores alternativos."""
        selectors = [
            "li.b_algo h2 a",
            "ol#b_results li.b_algo h2 a",
            "main ol#b_results h2 a",
        ]
        links: List[str] = []
        for selector in selectors:
            elements = driver.find_elements(By.CSS_SELECTOR, selector)
            for element in elements:
                href = element.get_attribute("href")
                if href and href.startswith("http"):
                    links.append(href)
            if links:
                break
        return links

    for page in range(1, max_pages + 1):
        search_url = _build_search_url(query, page)
        logger.info("Carregando resultados do Bing (página %d/%d)", page, max_pages)

        try:
            driver.get(search_url)
        except WebDriverException as exc:
            logger.error("Falha ao abrir o Bing na página %d: %s", page, exc)
            break

        _accept_consent_if_present()

        try:
            WebDriverWait(driver, 8).until(
                EC.presence_of_element_located((By.CSS_SELECTOR, "ol#b_results"))
            )
        except TimeoutException:
            logger.warning("Timeout esperando resultados na página %d", page)

        results = _find_results()
        if not results:
            logger.warning("Nenhum resultado orgânico encontrado na página %d", page)
            break

        collected.extend(results)

        if page >= max_pages:
            break

        try:
            driver.find_element(By.CSS_SELECTOR, "a.sb_pagN")
        except NoSuchElementException:
            logger.info("Página %d não possui paginação seguinte. Encerrando.", page)
            break

        delay = get_random_delay(REQUEST_DELAY_SECONDS_RANGE)
        logger.debug("Aguardando %.2f segundos antes da próxima página.", delay)
        time.sleep(delay)

    unique_urls = list(dict.fromkeys(collected))
    logger.info("Total de URLs coletadas: %d", len(unique_urls))
    return unique_urls
