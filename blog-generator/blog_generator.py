#!/usr/bin/env python3
"""
Blog Generator - DIAX CRM
Gera 1 post de blog por dia e publica via API.
Roda como cron job: 0 6 * * * /caminho/venv/bin/python /caminho/blog_generator.py

Fluxo:
  1. Pega proximo topico da fila (topics.json)
  2. Gera conteudo HTML via OpenAI
  3. Gera imagem via DALL-E 3 e faz upload para API (wwwroot/blog-images/)
  4. Publica via POST /api/v1/blog/admin
  5. Registra resultado no log
"""

import os
import sys
import json
import re
import base64
import unicodedata
import logging
import requests
from datetime import datetime
from pathlib import Path
from openai import OpenAI

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

OPENAI_API_KEY  = os.environ["OPENAI_API_KEY"]
DIAX_API_URL    = os.environ["DIAX_API_URL"]          # ex: https://api.alexandrequeiroz.com.br
DIAX_API_TOKEN  = os.environ["DIAX_API_TOKEN"]        # Bearer token JWT
AUTHOR_NAME     = os.environ.get("AUTHOR_NAME", "Alexandre Queiroz")

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
# GERACAO DE CONTEUDO (OpenAI)
# ---------------------------------------------
def generate_article(topic: dict) -> dict:
    """
    Retorna {"title": str, "content": str (HTML), "meta_description": str, "keywords": str}
    """
    client = OpenAI(api_key=OPENAI_API_KEY)

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

    log.info("Gerando artigo com OpenAI GPT-4o...")
    response = client.chat.completions.create(
        model="gpt-4o",
        messages=[
            {"role": "system", "content": system_prompt},
            {"role": "user",   "content": user_prompt},
        ],
        temperature=0.7,
        max_tokens=4000,
    )

    raw = response.choices[0].message.content.strip()

    # Remove possivel bloco markdown ```json ... ```
    raw = re.sub(r"^```json\s*", "", raw)
    raw = re.sub(r"^```\s*",    "", raw)
    raw = re.sub(r"\s*```$",    "", raw)

    article = json.loads(raw)
    log.info(f"Artigo gerado: '{article['title']}'")
    return article


# ---------------------------------------------
# GERACAO DE IMAGEM (DALL-E 3 + upload API)
# ---------------------------------------------
def generate_image_prompt(article_title: str, category: str) -> str:
    """Cria prompt editorial para a imagem."""
    client = OpenAI(api_key=OPENAI_API_KEY)
    response = client.chat.completions.create(
        model="gpt-4o-mini",
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
    )
    return response.choices[0].message.content.strip()


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
    Gera imagem via DALL-E 3, baixa e faz upload para a API.
    Retorna URL publica ou None se falhar.
    """
    try:
        client = OpenAI(api_key=OPENAI_API_KEY)
        img_prompt = generate_image_prompt(article_title, category)
        log.info(f"Gerando imagem: {img_prompt[:80]}...")

        img_response = client.images.generate(
            model="dall-e-3",
            prompt=img_prompt,
            size="1792x1024",
            quality="standard",
            n=1,
        )
        dalle_url = img_response.data[0].url

        # Baixa a imagem (URL do DALL-E expira em ~1h)
        log.info("Baixando imagem do DALL-E...")
        img_bytes = requests.get(dalle_url, timeout=60).content

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

    payload = {
        "title":              article["title"][:200],
        "slug":               slug[:250],
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

    headers = {
        "Authorization": f"Bearer {DIAX_API_TOKEN}",
        "Content-Type":  "application/json",
    }

    endpoint = f"{DIAX_API_URL.rstrip('/')}/api/v1/blog/admin"
    log.info(f"Publicando no endpoint: {endpoint}")

    resp = requests.post(endpoint, json=payload, headers=headers, timeout=30)

    if resp.status_code not in (200, 201):
        raise RuntimeError(
            f"API retornou {resp.status_code}: {resp.text[:500]}"
        )

    result = resp.json()
    log.info(f"Post publicado com sucesso! ID: {result.get('id')} | Slug: {result.get('slug')}")
    return result


# ---------------------------------------------
# MAIN
# ---------------------------------------------
def main():
    log.info("=" * 60)
    log.info(f"Blog Generator iniciado -- {datetime.now().strftime('%Y-%m-%d %H:%M')}")
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
