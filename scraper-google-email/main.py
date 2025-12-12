from __future__ import annotations

import argparse
import logging
import os
import sys
from typing import List, Sequence

from selenium import webdriver
from selenium.common.exceptions import WebDriverException
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.remote.webdriver import WebDriver

import config
from scraper.google_search import search_google
from scraper.crawler import PageContent, crawl_site_with_links, crawl_sites
from scraper.extractor import EmailRecord, extract_emails_and_company
from scraper.storage import save_site_emails, save_to_csv
from scraper.utils import LOGGER_NAME, setup_logger
from webdriver_manager.chrome import ChromeDriverManager


def parse_args(argv: Sequence[str]) -> argparse.Namespace:
    """Lê argumentos de linha de comando."""
    parser = argparse.ArgumentParser(description="Coleta de emails via busca ou site direto.")
    group = parser.add_mutually_exclusive_group()
    group.add_argument("-q", "--query", type=str, help="Termo de busca a ser usado.")
    group.add_argument(
        "-s",
        "--site",
        type=str,
        help="URL ou domínio para coletar emails diretamente desse site.",
    )
    parser.add_argument(
        "--headless",
        dest="headless",
        action="store_true",
        default=config.HEADLESS,
        help="Força execução em modo headless.",
    )
    parser.add_argument(
        "--no-headless",
        dest="headless",
        action="store_false",
        help="Desativa o modo headless para visualizar o navegador.",
    )
    return parser.parse_args(argv)


def _normalize_site_url(site: str) -> str:
    """Garante esquema padrão ao receber domínio ou URL."""
    cleaned = site.strip()
    if not cleaned:
        return ""
    if not cleaned.startswith(("http://", "https://")):
        return f"https://{cleaned}"
    return cleaned


def create_webdriver(headless: bool) -> WebDriver:
    """Inicializa o Chrome WebDriver com opções seguras."""
    chrome_options = Options()
    if headless:
        chrome_options.add_argument("--headless=new")
    chrome_options.add_argument("--disable-gpu")
    chrome_options.add_argument("--disable-dev-shm-usage")
    chrome_options.add_argument("--no-sandbox")
    chrome_options.add_argument("--window-size=1280,720")
    chrome_options.add_argument("--disable-blink-features=AutomationControlled")
    chrome_options.add_argument("--disable-extensions")
    chrome_options.add_argument(f"user-agent={config.USER_AGENT}")
    chrome_options.add_experimental_option("excludeSwitches", ["enable-automation"])
    chrome_options.add_experimental_option("useAutomationExtension", False)

    if config.USE_WEBDRIVER_MANAGER:
        driver_path = ChromeDriverManager(
            driver_version=config.CHROMEDRIVER_VERSION if config.CHROMEDRIVER_VERSION else None
        ).install()
        service = Service(driver_path)
    else:
        service = Service(config.WEBDRIVER_PATH) if config.WEBDRIVER_PATH else Service()

    driver = webdriver.Chrome(service=service, options=chrome_options)
    driver.set_page_load_timeout(config.PAGELOAD_TIMEOUT)
    return driver


def collect_emails(contents: List[PageContent]) -> List[EmailRecord]:
    """Percorre o HTML das páginas e extrai emails + nome/telefone/site."""
    records: List[EmailRecord] = []
    for page in contents:
        records.extend(extract_emails_and_company(page))
    return records


def deduplicate_records(records: List[EmailRecord]) -> List[EmailRecord]:
    """Remove duplicatas com base em (email, website)."""
    seen = set()
    unique: List[EmailRecord] = []
    for record in records:
        key = (record.email.lower(), record.website.lower())
        if key in seen:
            continue
        seen.add(key)
        unique.append(record)
    return unique


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_args(argv or sys.argv[1:])
    logger = setup_logger(logging.INFO)
    logger.debug("Parâmetros recebidos: %s", args)

    if args.site:
        site_url = _normalize_site_url(args.site)
        if not site_url:
            logger.error("Nenhum site fornecido. Encerrando.")
            return 1
        logger.info("Iniciando coleta direta no site: %s", site_url)
    else:
        query = args.query
        if not query:
            query = os.getenv("SCRAPER_QUERY", "").strip()
        if not query:
            try:
                query = input("Digite a query de busca: ").strip()
            except EOFError:
                logger.error("Nenhuma query fornecida e entrada padrão indisponível (STDIN fechado).")
                sys.exit(1)
        if not query:
            logger.error("Nenhuma query fornecida. Encerrando.")
            return 1
        logger.info("Iniciando busca no Google para: %s", query)

    try:
        driver = create_webdriver(headless=args.headless)
    except WebDriverException as exc:
        logger.error("Falha ao iniciar o WebDriver: %s", exc)
        return 1

    try:
        if args.site:
            contents = crawl_site_with_links(
                driver,
                site_url,
                max_pages=config.SITE_MAX_PAGES,
                max_depth=config.SITE_MAX_DEPTH,
                same_domain_only=config.SITE_SAME_DOMAIN_ONLY,
            )
        else:
            urls = search_google(driver, query, config.MAX_SEARCH_PAGES)
            if not urls:
                logger.warning("Nenhuma URL retornada pela busca.")
                return 0

            trimmed_urls = urls[: config.MAX_SITES_PER_RUN]
            logger.info("Total de URLs consideradas: %d", len(trimmed_urls))

            contents = crawl_sites(driver, trimmed_urls, config.MAX_SITES_PER_RUN)
    finally:
        driver.quit()

    if not contents:
        logger.warning("Nenhuma página acessada com sucesso.")
        return 0

    logger.info("Extraindo emails das páginas coletadas...")
    all_records = collect_emails(contents)
    unique_records = deduplicate_records(all_records)

    if not unique_records:
        logger.warning("Nenhum email encontrado.")
        return 0

    if args.site:
        save_site_emails(unique_records, site_url, config.OUTPUT_CSV_PATH)
    else:
        save_to_csv(unique_records, query, config.OUTPUT_CSV_PATH)

    logger.info("Visitas realizadas: %d", len(contents))
    logger.info("Emails únicos encontrados: %d", len(unique_records))
    logger.info("CSV gerado em: %s", config.OUTPUT_CSV_PATH)
    return 0


if __name__ == "__main__":
    sys.exit(main())
