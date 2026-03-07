# DIAX Blog Generator — Guia de Instalação na VPS

Gera e publica **1 post de blog por dia** na API DIAX via cron job Linux.
As imagens são salvas na sua pasta do Google Drive.

## Arquivos

```
blog-generator/
├── blog_generator.py     ← Script principal
├── topics.json           ← Fila de tópicos (editável)
├── requirements.txt      ← Dependências Python
├── .env.example          ← Template de variáveis
├── .env                  ← Suas credenciais (NÃO commitar)
├── service_account.json  ← Credencial Google (NÃO commitar)
└── logs/                 ← Gerado automaticamente
```

---

## 1. Configurar Google Drive (Service Account)

> Feito **uma única vez**. Sem OAuth — funciona 100% no cron automatizado.

1. Acesse [console.cloud.google.com](https://console.cloud.google.com)
2. Crie um projeto (ex: **diax-blog**)
3. **APIs e Serviços → Biblioteca** → ative a **Google Drive API**
4. **Credenciais → Criar credencial → Conta de serviço**
   - Nome: `blog-generator` → Concluído
5. Clique na conta criada → **Chaves → Adicionar chave → JSON**
   - Baixa o `service_account.json`
6. **Compartilhe a pasta do Drive com o email da service account:**
   - Abra: https://drive.google.com/drive/folders/1vv-w8rR4ew0oyhj4vI2ArXYjBR6EaI8W
   - Compartilhar → cole o email (ex: `blog-generator@diax-blog.iam.gserviceaccount.com`)
   - Permissão: **Editor**

---

## 2. Instalação na VPS (Ubuntu/Debian)

```bash
# Copiar arquivos para a VPS
scp -r blog-generator/ usuario@ip-da-vps:/opt/diax-blog/
scp service_account.json usuario@ip-da-vps:/opt/diax-blog/

# Na VPS
cd /opt/diax-blog
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt

# Configurar variáveis
cp .env.example .env
nano .env
```

Preencha no `.env`:
- `OPENAI_API_KEY` — sua chave OpenAI
- `DIAX_API_TOKEN` — Bearer token JWT (login na API)
- `GOOGLE_SA_JSON_PATH=/opt/diax-blog/service_account.json`
- `GOOGLE_DRIVE_FOLDER_ID=1vv-w8rR4ew0oyhj4vI2ArXYjBR6EaI8W` (já preenchido)

---

## 3. Testar manualmente

```bash
source venv/bin/activate
python blog_generator.py
```

Verifique o post em: `https://api.alexandrequeiroz.com.br/api/v1/blog/public`

---

## 4. Cron — 1 post por dia às 6h

```bash
crontab -e
```

Adicione:
```
0 6 * * * /opt/diax-blog/venv/bin/python /opt/diax-blog/blog_generator.py >> /opt/diax-blog/logs/cron.log 2>&1
```

---

## Como funciona o `topics.json`

| Campo | Descrição |
|---|---|
| `title` | Tema do artigo (input para o GPT) |
| `keyword` | Palavra-chave SEO principal |
| `slug` | URL do post (auto-gerado se vazio) |
| `category` | Categoria do post |
| `tags` | Tags separadas por vírgula |
| `city` | Cidade para contextualização |
| `status` | `pendente` → `em_progresso` → `publicado` |

Para adicionar tópicos, insira objetos com `"status": "pendente"`.

---

## Monitoramento

```bash
# Ver tópicos restantes
python3 -c "import json; d=json.load(open('topics.json')); print(sum(1 for t in d['topics'] if t['status']=='pendente'), 'pendentes')"

# Acompanhar log em tempo real
tail -f logs/cron.log
```

Se um post **falhar**, o tópico volta para `"status": "pendente"` automaticamente.

---

## Custo estimado por post

| Serviço | Custo |
|---|---|
| GPT-4o (geração artigo) | ~$0.02 |
| DALL-E 3 (imagem) | ~$0.08 |
| Google Drive | Gratuito |
| **Total por post** | **~$0.10** |
| **Por mês (30 posts)** | **~$3.00** |
