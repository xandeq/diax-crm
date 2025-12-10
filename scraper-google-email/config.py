from __future__ import annotations

import os
from typing import Tuple

from dotenv import load_dotenv

load_dotenv()

# Base URL do Google
GOOGLE_SEARCH_URL_BASE = "https://www.google.com/search"

# Opções de execução
HEADLESS = os.getenv("HEADLESS", "true").lower() == "true"
MAX_SEARCH_PAGES = int(os.getenv("MAX_SEARCH_PAGES", "3"))
MAX_SITES_PER_RUN = int(os.getenv("MAX_SITES_PER_RUN", "100"))
USE_WEBDRIVER_MANAGER = os.getenv("USE_WEBDRIVER_MANAGER", "true").lower() == "true"

# Delay aleatório entre requisições
REQUEST_DELAY_SECONDS_RANGE: Tuple[float, float] = (
    float(os.getenv("REQUEST_DELAY_MIN", "2")),
    float(os.getenv("REQUEST_DELAY_MAX", "5")),
)

# Saída
OUTPUT_CSV_PATH = os.getenv("OUTPUT_CSV_PATH", "output/emails_result.csv")

# Caminho opcional do WebDriver (se não estiver no PATH)
WEBDRIVER_PATH = os.getenv("WEBDRIVER_PATH") or None
CHROMEDRIVER_VERSION = os.getenv("CHROMEDRIVER_VERSION") or ""

# User-Agent para minimizar bloqueios
USER_AGENT = os.getenv(
    "USER_AGENT",
    (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/124.0.0.0 Safari/537.36"
    ),
)

# Timeout padrão para carregamento de páginas (segundos)
PAGELOAD_TIMEOUT = int(os.getenv("PAGELOAD_TIMEOUT", "20"))
