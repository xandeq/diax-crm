from __future__ import annotations

import logging
import time
from collections import deque
from dataclasses import dataclass
from typing import List, Set, Tuple
from urllib.parse import urljoin, urlparse

from selenium.common.exceptions import TimeoutException, WebDriverException
from selenium.webdriver.remote.webdriver import WebDriver

from bs4 import BeautifulSoup

from config import REQUEST_DELAY_SECONDS_RANGE
from .utils import LOGGER_NAME, get_random_delay


@dataclass
class PageContent:
    """Representa o HTML obtido de uma URL específica."""

    url: str
    html: str


def _normalize_host(url: str) -> str:
    """Remove esquema e www para comparação de domínio."""
    parsed = urlparse(url)
    host = parsed.netloc or parsed.path
    return host.lower().lstrip("www.")


def _should_follow(link: str, base_host: str, same_domain_only: bool) -> bool:
    parsed = urlparse(link)
    if parsed.scheme not in {"http", "https"}:
        return False
    if same_domain_only and base_host:
        return _normalize_host(link) == base_host
    return True


def _collect_links(html: str, current_url: str, base_host: str, same_domain_only: bool) -> List[str]:
    """Extrai hrefs da página, normaliza e filtra por domínio."""
    soup = BeautifulSoup(html, "lxml")
    links: List[str] = []
    for anchor in soup.find_all("a", href=True):
        href = anchor["href"].strip()
        if href.startswith(("#", "mailto:", "javascript:", "tel:", "sms:")):
            continue
        absolute = urljoin(current_url, href)
        absolute = absolute.split("#", 1)[0]
        if _should_follow(absolute, base_host, same_domain_only):
            links.append(absolute)
    # Remove duplicados mantendo ordem
    seen: Set[str] = set()
    unique = []
    for link in links:
        if link in seen:
            continue
        seen.add(link)
        unique.append(link)
    return unique


def crawl_sites(driver: WebDriver, urls: List[str], max_sites: int) -> List[PageContent]:
    """Visita URLs, coleta page_source e ignora falhas individuais."""
    logger = logging.getLogger(LOGGER_NAME)
    contents: List[PageContent] = []

    for index, url in enumerate(urls[:max_sites], start=1):
        delay = get_random_delay(REQUEST_DELAY_SECONDS_RANGE)
        logger.debug("Delay de %.2fs antes de visitar %s", delay, url)
        time.sleep(delay)

        logger.info("Visitando site %d/%d: %s", index, min(len(urls), max_sites), url)
        try:
            driver.get(url)
            html = driver.page_source or ""
            contents.append(PageContent(url=url, html=html))
        except TimeoutException as exc:
            logger.warning("Timeout ao carregar %s: %s", url, exc)
        except WebDriverException as exc:
            logger.warning("Erro ao acessar %s: %s", url, exc)
        except Exception as exc:  # noqa: BLE001
            logger.error("Erro inesperado em %s: %s", url, exc)

    return contents


def crawl_site_with_links(
    driver: WebDriver,
    start_url: str,
    max_pages: int,
    max_depth: int,
    same_domain_only: bool = True,
) -> List[PageContent]:
    """
    Percorre o site a partir de start_url, seguindo links internos até os limites.
    - max_pages: número máximo de páginas a visitar.
    - max_depth: profundidade máxima de links (0 = apenas a página inicial).
    """
    logger = logging.getLogger(LOGGER_NAME)
    base_host = _normalize_host(start_url)
    queue: deque[Tuple[str, int]] = deque([(start_url, 0)])
    visited: Set[str] = set()
    contents: List[PageContent] = []

    while queue and len(contents) < max_pages:
        url, depth = queue.popleft()
        if url in visited:
            continue
        visited.add(url)

        delay = get_random_delay(REQUEST_DELAY_SECONDS_RANGE)
        logger.debug("Delay de %.2fs antes de visitar %s (profundidade %d)", delay, url, depth)
        time.sleep(delay)

        logger.info(
            "Visitando página %d/%d (profundidade %d): %s",
            len(contents) + 1,
            max_pages,
            depth,
            url,
        )
        try:
            driver.get(url)
            html = driver.page_source or ""
            contents.append(PageContent(url=url, html=html))
        except TimeoutException as exc:
            logger.warning("Timeout ao carregar %s: %s", url, exc)
            continue
        except WebDriverException as exc:
            logger.warning("Erro ao acessar %s: %s", url, exc)
            continue
        except Exception as exc:  # noqa: BLE001
            logger.error("Erro inesperado em %s: %s", url, exc)
            continue

        if depth >= max_depth:
            continue

        links = _collect_links(html, url, base_host, same_domain_only)
        for link in links:
            if len(contents) + len(queue) >= max_pages:
                break
            if link in visited:
                continue
            queue.append((link, depth + 1))

    return contents
