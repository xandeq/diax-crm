#!/usr/bin/env python3
"""
Blog Generator - DIAX CRM
Gera 1 post de blog por dia e publica via API.
Roda como cron job: 0 6 * * * /caminho/venv/bin/python /caminho/blog_generator.py

Providers:
  - Texto: OpenRouter (DeepSeek Chat) — barato e excelente em PT-BR
  - Imagem: FAL.AI (Flux) — alta qualidade, ~$0.03/imagem
  - Fallback texto: OpenAI GPT-4o (se OPENAI_API_KEY estiver configurada)

Fluxo:
  1. Pega proximo topico da fila (topics.json)
  2. Gera conteudo HTML via OpenRouter/DeepSeek
  3. Gera imagem via FAL.AI Flux e faz upload para API (wwwroot/blog-images/)
  4. Publica via POST /api/v1/blog/admin
  5. Registra resultado no log
"""

import os
import sys
import json
import re
import base64
import time
import unicodedata
import logging
import requests
from datetime import datetime
from pathlib import Path

# ---------------------------------------------
# CONFIGURACAO
# ---------------------------------------------
BASE_DIR = Path(__file__).parent

# Carrega .env manual (sem dependencias extras)
env_file = BASE_DIR / ".env"
if env_file.exists():
    for line in env_file.read_text().splitlines():
        line = line.strip()
        if line and not line.startswith("#") and "=" in line:
            key, _, value = line.partition("=")
            os.environ.setdefault(key.strip(), value.strip().strip('"'))

# Providers
OPENROUTER_API_KEY = os.environ.get("OPENROUTER_API_KEY", "")
FAL_KEY            = os.environ.get("FAL_KEY", "")
OPENAI_API_KEY     = os.environ.get("OPENAI_API_KEY", "")  # fallback

# DIAX API
DIAX_API_URL    = os.environ["DIAX_API_URL"]
DIAX_API_TOKEN  = os.environ["DIAX_API_TOKEN"]
AUTHOR_NAME     = os.environ.get("AUTHOR_NAME", "Alexandre Queiroz")

# Modelo de texto (OpenRouter)
LLM_MODEL       = os.environ.get("LLM_MODEL", "deepseek/deepseek-chat")
# Modelo de imagem (FAL.AI)
IMAGE_MODEL     = os.environ.get("IMAGE_MODEL", "fal-ai/flux/schnell")

TOPICS_FILE  = BASE_DIR / "topics.json"
LOG_FILE     = BASE_DIR / "logs" / f"blog_{datetime.now().strftime('%Y-%m')}.log"

# ---------------------------------------------
# LOGGING
# ---------------------------------------------
LOG_FILE.parent.mkdir(exist_ok=True)
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s | %(levelname)s | %(message)s",
    handlers=[
        logging.FileHandler(LOG_FILE, encoding="utf-8"),
        logging.StreamHandler(sys.stdout),
    ],
)
log = logging.getLogger(__name__)

# ---------------------------------------------
# HELPERS
# ---------------------------------------------
def to_slug(text: str) -> str:
    """Gera slug SEO-friendly a partir de um texto."""
    text = unicodedata.normalize("NFD", text)
    text = "".join(c for c in text if unicodedata.category(c) != "Mn")
    text = text.lower()
    text = re.sub(r"[^a-z0-9\s-]", "", text)
    text = re.sub(r"\s+", "-", text.strip())
    text = re.sub(r"-+", "-", text)
    return text.strip("-")


def load_topics() -> dict:
    if not TOPICS_FILE.exists():
        log.error(f"Arquivo de topicos nao encontrado: {TOPICS_FILE}")
        sys.exit(1)
    return json.loads(TOPICS_FILE.read_text(encoding="utf-8"))


def save_topics(data: dict):
    TOPICS_FILE.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


def get_next_topic() -> dict | None:
    """Retorna o proximo topico pendente e marca como 'em_progresso'."""
    data = load_topics()
    for topic in data["topics"]:
        if topic.get("status") == "pendente":
            topic["status"] = "em_progresso"
            save_topics(data)
            log.info(f"Topico selecionado: {topic['title']}")
            return topic
    return None


def mark_topic_done(title: str, post_id: str, slug: str):
    data = load_topics()
    for topic in data["topics"]:
        if topic["title"] == title:
            topic["status"] = "publicado"
            topic["post_id"] = post_id
            topic["slug"] = slug
            topic["published_at"] = datetime.now().isoformat()
            break
    save_topics(data)


def mark_topic_failed(title: str, error: str):
    data = load_topics()
    for topic in data["topics"]:
        if topic["title"] == title:
            topic["status"] = "pendente"  # Volta para fila
            topic["last_error"] = error
            break
    save_topics(data)


# ---------------------------------------------
# LLM CALL (OpenRouter ou OpenAI fallback)
# ---------------------------------------------
def llm_chat(messages: list[dict], max_tokens: int = 4000, temperature: float = 0.7) -> str:
    """
    Chama LLM via OpenRouter (principal) ou OpenAI (fallback).
    Retorna o texto da resposta.
    """
    if OPENROUTER_API_KEY:
        log.info(f"Chamando OpenRouter ({LLM_MODEL})...")
        resp = requests.post(
            "https://openrouter.ai/api/v1/chat/completions",
            headers={
                "Authorization": f"Bearer {OPENROUTER_API_KEY}",
                "Content-Type": "application/json",
            },
            json={
                "model": LLM_MODEL,
                "messages": messages,
                "max_tokens": max_tokens,
                "temperature": temperature,
            },
            timeout=120,
        )
        resp.raise_for_status()
        data = resp.json()
        return data["choices"][0]["message"]["content"].strip()

    elif OPENAI_API_KEY:
        log.info("Fallback: chamando OpenAI GPT-4o...")
        from openai import OpenAI
        client = OpenAI(api_key=OPENAI_API_KEY)
        response = client.chat.completions.create(
            model="gpt-4o",
            messages=messages,
            temperature=temperature,
            max_tokens=max_tokens,
        )
        return response.choices[0].message.content.strip()

    else:
        raise RuntimeError("Nenhuma API key configurada (OPENROUTER_API_KEY ou OPENAI_API_KEY)")


# ---------------------------------------------
# GERACAO DE CONTEUDO
# ---------------------------------------------
def generate_article(topic: dict) -> dict:
    """
    Retorna {"title": str, "content": str (HTML), "meta_description": str, "keywords": str}
    """
    prompt_topic    = topic["title"]
    prompt_keyword  = topic.get("keyword", prompt_topic)
    prompt_category = topic.get("category", "Marketing Digital")
    prompt_city     = topic.get("city", "Vitoria, ES")

    system_prompt = (
        "Voce e um especialista em SEO e marketing digital no Brasil. "
        "Escreve artigos em Portugues do Brasil, otimizados para Google, "
        "com foco em clientes locais de Vitoria e regiao do Espirito Santo. "
        "Tom: profissional, direto, util e confiavel."
    )

    user_prompt = f"""
Escreva um artigo de blog completo sobre: "{prompt_topic}"

Palavra-chave principal: {prompt_keyword}
Cidade/Regiao: {prompt_city}
Categoria: {prompt_category}

Retorne APENAS um objeto JSON valido com esta estrutura (sem markdown ao redor):
{{
  "title": "[Titulo SEO otimizado, max 60 caracteres]",
  "meta_description": "[Descricao para Google, entre 140 e 160 caracteres]",
  "keywords": "[5 a 8 palavras-chave separadas por virgula]",
  "content": "[Corpo do artigo em HTML limpo. Use apenas: <p>, <h2>, <h3>, <ul>, <li>, <strong>, <em>. SEM scripts, SEM estilos inline, SEM comentarios HTML]"
}}

Regras do artigo:
- Entre 1200 e 1800 palavras
- Titulo principal dentro de um unico <h2> no inicio do conteudo
- Gancho com no maximo 3 frases no primeiro <p>
- Subtitulos usando <h2> e <h3>
- Pelo menos 3 dicas praticas em formato <ul><li>
- Mencione {prompt_city} naturalmente no texto
- Termine com um paragrafo de call-to-action chamando para contato com a agencia
- Use a palavra-chave principal "{prompt_keyword}" de forma natural, sem exagero
""".strip()

    raw = llm_chat(
        messages=[
            {"role": "system", "content": system_prompt},
            {"role": "user",   "content": user_prompt},
        ],
        max_tokens=4000,
        temperature=0.7,
    )

    # Remove possivel bloco markdown ```json ... ```
    raw = re.sub(r"^```json\s*", "", raw)
    raw = re.sub(r"^```\s*",    "", raw)
    raw = re.sub(r"\s*```$",    "", raw)

    article = json.loads(raw)
    log.info(f"Artigo gerado: '{article['title']}'")
    return article


# ---------------------------------------------
# GERACAO DE IMAGEM (FAL.AI Flux ou DALL-E fallback)
# ---------------------------------------------
def generate_image_prompt(article_title: str, category: str) -> str:
    """Cria prompt editorial para a imagem usando a LLM configurada."""
    return llm_chat(
        messages=[
            {
                "role": "system",
                "content": "You create short, cinematic editorial image prompts in English for blog featured images. No text, no logos. Photorealistic style.",
            },
            {
                "role": "user",
                "content": f"Create one paragraph image prompt for a blog post titled: '{article_title}' (category: {category}). Professional, clean, Brazilian business context.",
            },
        ],
        max_tokens=150,
        temperature=0.8,
    )


def generate_image_fal(prompt: str) -> bytes | None:
    """
    Gera imagem via FAL.AI (Flux Schnell).
    Usa queue API: submit → poll → download.
    """
    if not FAL_KEY:
        return None

    log.info(f"Gerando imagem via FAL.AI ({IMAGE_MODEL})...")

    # 1. Submit job
    submit_resp = requests.post(
        f"https://queue.fal.run/{IMAGE_MODEL}",
        headers={
            "Authorization": f"Key {FAL_KEY}",
            "Content-Type": "application/json",
        },
        json={
            "prompt": prompt,
            "image_size": "landscape_16_9",
            "num_images": 1,
        },
        timeout=30,
    )
    submit_resp.raise_for_status()
    job = submit_resp.json()
    request_id = job.get("request_id")

    if not request_id:
        # Resposta sincrona (resultado direto)
        images = job.get("images", [])
        if images:
            img_url = images[0].get("url")
            if img_url:
                return requests.get(img_url, timeout=60).content
        return None

    # 2. Poll for result (usa URLs retornadas pelo submit, nao constroi manualmente)
    status_url = job.get("status_url", f"https://queue.fal.run/{IMAGE_MODEL}/requests/{request_id}/status")
    result_url = job.get("response_url", f"https://queue.fal.run/{IMAGE_MODEL}/requests/{request_id}")

    for _ in range(60):  # max 60 tentativas (5 min)
        time.sleep(5)
        status_resp = requests.get(
            status_url,
            headers={"Authorization": f"Key {FAL_KEY}"},
            timeout=15,
        )
        status_data = status_resp.json()
        status = status_data.get("status")

        if status == "COMPLETED":
            result_resp = requests.get(
                result_url,
                headers={"Authorization": f"Key {FAL_KEY}"},
                timeout=30,
            )
            result_data = result_resp.json()
            images = result_data.get("images", [])
            if images:
                img_url = images[0].get("url")
                if img_url:
                    log.info(f"Imagem gerada: {img_url[:80]}...")
                    return requests.get(img_url, timeout=60).content
            return None

        elif status in ("FAILED", "CANCELLED"):
            log.warning(f"FAL.AI job falhou: {status_data}")
            return None

    log.warning("FAL.AI timeout apos 5 minutos")
    return None


def upload_image_to_api(img_bytes: bytes, filename: str) -> str | None:
    """
    Faz upload da imagem para a API DIAX (salva em wwwroot/blog-images/).
    Retorna URL publica ou None se falhar.
    """
    try:
        b64 = base64.b64encode(img_bytes).decode("utf-8")
        payload = {
            "base64Content": b64,
            "fileName": filename,
        }
        headers = {
            "Authorization": f"Bearer {DIAX_API_TOKEN}",
            "Content-Type": "application/json",
        }
        endpoint = f"{DIAX_API_URL.rstrip('/')}/api/v1/blog/admin/upload-image"
        log.info(f"Fazendo upload da imagem para: {endpoint}")

        resp = requests.post(endpoint, json=payload, headers=headers, timeout=60)

        if resp.status_code != 200:
            log.warning(f"Upload falhou ({resp.status_code}): {resp.text[:300]}")
            return None

        result = resp.json()
        url = result.get("url")
        log.info(f"Imagem hospedada: {url}")
        return url

    except Exception as e:
        log.warning(f"Falha ao fazer upload da imagem: {e}")
        return None


def generate_and_upload_image(article_title: str, category: str) -> str | None:
    """
    Gera imagem via FAL.AI (ou DALL-E fallback), baixa e faz upload para a API.
    Retorna URL publica ou None se falhar.
    """
    try:
        img_prompt = generate_image_prompt(article_title, category)
        log.info(f"Prompt de imagem: {img_prompt[:80]}...")

        img_bytes = None

        # Tenta FAL.AI primeiro
        if FAL_KEY:
            img_bytes = generate_image_fal(img_prompt)

        # Fallback: DALL-E 3 (se OpenAI key disponivel)
        if img_bytes is None and OPENAI_API_KEY:
            log.info("Fallback: gerando imagem via DALL-E 3...")
            from openai import OpenAI
            client = OpenAI(api_key=OPENAI_API_KEY)
            img_response = client.images.generate(
                model="dall-e-3",
                prompt=img_prompt,
                size="1792x1024",
                quality="standard",
                n=1,
            )
            dalle_url = img_response.data[0].url
            img_bytes = requests.get(dalle_url, timeout=60).content

        if img_bytes is None:
            log.warning("Nenhum provider de imagem disponivel. Publicando sem imagem.")
            return None

        # Upload para API (salva em wwwroot/blog-images/)
        slug = to_slug(article_title)
        filename = f"{slug}-{datetime.now().strftime('%Y%m%d')}.jpg"
        return upload_image_to_api(img_bytes, filename)

    except Exception as e:
        log.warning(f"Falha ao gerar/hospedar imagem: {e}. Publicando sem imagem.")
        return None


# ---------------------------------------------
# PUBLICACAO NA API DIAX
# ---------------------------------------------
def publish_to_api(article: dict, topic: dict, image_url: str | None) -> dict:
    """
    POST /api/v1/blog/admin
    Retorna o body da resposta da API.
    """
    slug  = topic.get("slug") or to_slug(article["title"])
    tags  = topic.get("tags", "")
    category = topic.get("category", "Marketing Digital")

    # Excerpt = primeiros 160 chars do conteudo sem HTML
    plain_text = re.sub(r"<[^>]+>", "", article["content"])
    excerpt    = plain_text[:157].strip() + "..."

    headers = {
        "Authorization": f"Bearer {DIAX_API_TOKEN}",
        "Content-Type":  "application/json",
    }

    endpoint = f"{DIAX_API_URL.rstrip('/')}/api/v1/blog/admin"

    # Tenta publicar; se slug duplicado (409), adiciona sufixo com data
    for attempt in range(3):
        current_slug = slug if attempt == 0 else f"{slug}-{datetime.now().strftime('%Y%m%d')}-{attempt}"

        payload = {
            "title":              article["title"][:200],
            "slug":               current_slug[:250],
            "contentHtml":        article["content"],
            "excerpt":            excerpt[:500],
            "metaTitle":          article["title"][:70],
            "metaDescription":    article["meta_description"][:160],
            "keywords":           article["keywords"][:500],
            "authorName":         AUTHOR_NAME[:100],
            "featuredImageUrl":   image_url,
            "category":           category[:100] if category else None,
            "tags":               tags[:500] if tags else None,
            "publishImmediately": True,
        }

        log.info(f"Publicando no endpoint: {endpoint} (slug: {current_slug})")
        resp = requests.post(endpoint, json=payload, headers=headers, timeout=30)

        if resp.status_code == 409:
            log.warning(f"Slug '{current_slug}' ja existe. Tentando com sufixo...")
            continue

        if resp.status_code not in (200, 201):
            raise RuntimeError(
                f"API retornou {resp.status_code}: {resp.text[:500]}"
            )

        result = resp.json()
        log.info(f"Post publicado com sucesso! ID: {result.get('id')} | Slug: {result.get('slug')}")
        return result

    raise RuntimeError(f"Slug '{slug}' duplicado apos 3 tentativas.")


# ---------------------------------------------
# MAIN
# ---------------------------------------------
def main():
    log.info("=" * 60)
    log.info(f"Blog Generator iniciado -- {datetime.now().strftime('%Y-%m-%d %H:%M')}")
    log.info(f"LLM: {LLM_MODEL if OPENROUTER_API_KEY else 'OpenAI GPT-4o (fallback)'}")
    log.info(f"Imagem: {IMAGE_MODEL if FAL_KEY else 'DALL-E 3 (fallback)' if OPENAI_API_KEY else 'Nenhum'}")
    log.info("=" * 60)

    topic = get_next_topic()
    if not topic:
        log.info("Nenhum topico pendente na fila. Encerrando.")
        sys.exit(0)

    try:
        # 1. Gera conteudo
        article = generate_article(topic)

        # 2. Gera imagem (opcional -- nao bloqueia se falhar)
        image_url = generate_and_upload_image(
            article["title"],
            topic.get("category", "Marketing Digital")
        )

        # 3. Publica na API
        result = publish_to_api(article, topic, image_url)

        # 4. Marca como publicado
        mark_topic_done(topic["title"], result.get("id", ""), result.get("slug", ""))

        log.info(f"Sucesso! URL: /blog/{result.get('slug')}")

    except Exception as e:
        log.error(f"Erro ao processar topico '{topic['title']}': {e}", exc_info=True)
        mark_topic_failed(topic["title"], str(e))
        sys.exit(1)


if __name__ == "__main__":
    main()
