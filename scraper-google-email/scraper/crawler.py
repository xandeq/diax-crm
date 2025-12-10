from __future__ import annotations

import logging
import time
from dataclasses import dataclass
from typing import List

from selenium.common.exceptions import TimeoutException, WebDriverException
from selenium.webdriver.remote.webdriver import WebDriver

from config import REQUEST_DELAY_SECONDS_RANGE
from .utils import LOGGER_NAME, get_random_delay


@dataclass
class PageContent:
    """Representa o HTML obtido de uma URL específica."""

    url: str
    html: str


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
