# Google Email Scraper

Projeto em Python para buscar termos no Google, coletar links orgânicos, visitar cada site, extrair emails, nome da empresa, telefone e site e salvar os resultados em CSV.
Módulo responsável pela coleta automática de leads a partir de buscas no Google.  
Este serviço funciona como um **worker independente**, especializado em localizar informações públicas de empresas e pessoas a partir de pesquisas definidas no CRM ou pelo n8n.
Ele faz parte do ecossistema **Digital Controller (DIAX CRM)**.

## Requisitos

- Python 3.11+
- Google Chrome instalado
- Chromedriver compatível. Por padrão é baixado automaticamente via `webdriver-manager`. Você pode fornecer um driver próprio via variável de ambiente `WEBDRIVER_PATH` e definir `USE_WEBDRIVER_MANAGER=false` se preferir. Para fixar uma versão específica do driver pelo webdriver-manager, use `CHROMEDRIVER_VERSION=142.0.0` (opcional).

Instalação das dependências:

```bash
pip install -r requirements.txt
```

## Uso rápido

```bash
python main.py --query "advogados empresariais Vitória ES"
```

Argumentos úteis:
- `--query` / `-q`: termo de busca. Se omitido, será solicitado via `input()`.
- `--no-headless`: executa o Chrome com interface. Por padrão, usa modo headless.

O resultado será salvo em `output/<slug>-<ddmmyyyy>-<hhmmss>.csv` (ex.: `advogados_empresariais_Vitória_ES-09122025-154455.csv`) contendo:
`company_name`, `email`, `phone`, `website`, `source_url`, `search_query`, `timestamp`.

## Estrutura

- `config.py`: configurações padrão e leitura opcional de variáveis de ambiente.
- `main.py`: fluxo principal (busca, crawl, extração, salvamento).
- `scraper/`: módulos especializados:
  - `google_search.py`: monta URLs e coleta links orgânicos do Google.
  - `crawler.py`: visita sites e captura o HTML.
  - `extractor.py`: extrai emails, tenta identificar nome da empresa e telefone.
  - `storage.py`: garante pasta de saída e grava CSV.
  - `utils.py`: logging, delays e normalização básica.

## Observações de uso

- A busca percorre até `MAX_SEARCH_PAGES` páginas do Google.
- O crawler visita no máximo `MAX_SITES_PER_RUN` URLs por execução.
- Delays aleatórios entre requisições reduzem risco de bloqueio.
- O modo headless é padrão (`HEADLESS=True` em `config.py`), mas pode ser desativado via CLI ou variável de ambiente `HEADLESS=false`.
- O webdriver é gerenciado automaticamente (`USE_WEBDRIVER_MANAGER=true`). Para usar um driver já instalado, defina `USE_WEBDRIVER_MANAGER=false` e `WEBDRIVER_PATH` para o executável adequado.

## Considerações legais e éticas

- Este código é apenas para fins educacionais.
- Respeite sempre `robots.txt`, termos de uso dos sites e legislações de proteção de dados (LGPD/GDPR).
- Coleta e uso de emails devem seguir consentimento/opt-in e demais obrigações legais aplicáveis.
- Realize scraping apenas em conteúdo público e de forma responsável.

# Objetivo

Extrair informações de contato de sites encontrados através de buscas no Google e registrar leads de forma automática e contínua.

O foco principal é coletar:

- Email
- Nome da empresa ou pessoa
- Telefone (quando presente)
- URL do site de onde os dados foram extraídos
- Query utilizada
- Segmento (informado pelo CRM ou campanha)
- Data e contexto da coleta

O módulo foi desenhado para operar totalmente independente da interface do usuário, sendo acionado por:

- CRM (via API)
- Motor de automação n8n
- Linha de comando
- Scripts ou outras integrações

---

