from __future__ import annotations

import logging
import random
import re
from typing import Tuple

LOGGER_NAME = "email_scraper"


def setup_logger(level: int = logging.INFO) -> logging.Logger:
    """Configura logger padrão do projeto."""
    logger = logging.getLogger(LOGGER_NAME)
    if logger.handlers:
        logger.setLevel(level)
        return logger

    handler = logging.StreamHandler()
    formatter = logging.Formatter("%(asctime)s | %(levelname)s | %(message)s")
    handler.setFormatter(formatter)
    logger.addHandler(handler)
    logger.setLevel(level)
    logger.propagate = False
    return logger


def get_random_delay(range_seconds: Tuple[float, float]) -> float:
    """Retorna um valor aleatório dentro do intervalo informado."""
    low, high = range_seconds
    return random.uniform(low, high)


def normalize_text(value: str) -> str:
    """Remove espaçamentos excessivos em textos curtos."""
    return re.sub(r"\s+", " ", value).strip()
