export type AppType =
  | 'SaaS'
  | 'Pipeline'
  | 'Bot/Automação'
  | 'Scraper'
  | 'Projeto'
  | 'Infraestrutura'
  | 'Conteúdo'
  | 'Scripts'
  | 'Ferramenta'
  | 'Placeholder';

export interface AppEntry {
  folder: string;
  type: AppType;
  stack: string;
  description: string;
}

export const APPS_LAST_UPDATED = '2026-05-14';

export const appsInventory: AppEntry[] = [
  { folder: 'alecook', type: 'SaaS', stack: '.NET 8 + React/Vite', description: 'App de receitas culinárias. API + frontend. Deploy via GitHub Actions → SmarterASP' },
  { folder: 'apartamentos-alugar-vv', type: 'Ferramenta', stack: 'Python', description: 'Buscador de apartamentos para aluguel em Vila Velha. Gera relatórios com opções filtradas' },
  { folder: 'automatic-applier', type: 'Bot/Automação', stack: 'Python', description: 'Candidatura automática a vagas. Versão mais antiga do easy-apply-bot' },
  { folder: 'bairronow', type: 'SaaS', stack: '.NET 8 + React + Mobile', description: '"Nextdoor brasileiro" — rede social de bairro com verificação de endereço, marketplace e síndicos' },
  { folder: 'blog-app', type: 'Projeto', stack: 'React CRA + API', description: 'App de blog genérico. Frontend + API separados' },
  { folder: 'carrosseis', type: 'Placeholder', stack: '—', description: 'Pasta vazia — possivelmente para assets de carrosséis do Instagram' },
  { folder: 'chat-app', type: 'Projeto', stack: 'React Native', description: 'Aplicativo de chat mobile. Em estágio inicial' },
  { folder: 'cleardesk', type: 'SaaS', stack: '.NET + Python + Playwright', description: 'SaaS de gestão de tarefas/projetos. Backend .NET + testes e2e + scripts de deploy FTP' },
  { folder: 'content-engine', type: 'Pipeline', stack: 'Python', description: 'PIPELINE UNIFICADO — IG + FB + LinkedIn + YouTube + TikTok. Multi-brand (YAML). 6 agentes: trend→competitor→ideator→scripter→generators→publishers. Vídeo: Kling/Veo3/Remotion. TTS ElevenLabs. Schedule 3×/dia.' },
  { folder: 'content-pipeline', type: 'Pipeline', stack: 'Python', description: '[LEGADO — substituído pelo content-engine] Pipeline de conteúdo 6 agentes. Manter como referência.' },
  { folder: 'crm-trabalho', type: 'Projeto', stack: 'Angular 12', description: 'CRM simples para controle de trabalho. Versão legada, provavelmente obsoleta' },
  { folder: 'diax-crm', type: 'SaaS', stack: 'Next.js 14 + .NET 8 + n8n', description: 'Monorepo do DIAX CRM — CRM privado da Alexandre Queiroz Marketing Digital. Inclui n8n workflows e scraper Google Maps' },
  { folder: 'dotnet-crawler', type: 'Ferramenta', stack: '.NET Core', description: 'Framework de web crawling modular (Request, Processor, Pipeline, Scheduler). Baseado em projeto open-source' },
  { folder: 'easy-apply-bot', type: 'Bot/Automação', stack: 'Python + Selenium', description: 'Bot LinkedIn Easy Apply + agregação multi-source de vagas. Tem analytics dashboard e AI question handler' },
  { folder: 'ebook-vaga-na-gringa', type: 'Conteúdo', stack: 'Markdown + imagens', description: 'E-book "Vaga na Gringa" — conteúdo, banners Kiwify, assets de anúncios. Produto digital' },
  { folder: 'email-crawler', type: 'Scraper', stack: 'Python + PostgreSQL', description: 'Extrator de emails de websites. Tem banco de dados e docs' },
  { folder: 'email-marketing-n8n', type: 'Pipeline', stack: 'n8n + JS', description: 'Especificações e guias de implementação de email marketing via n8n' },
  { folder: 'extrator-diax', type: 'SaaS', stack: 'Flask + PostgreSQL + Traefik', description: 'Sistema de extração de leads (emails, tel, WhatsApp, CNPJ). 7 métodos paralelos. Deploy na VPS. Domínio: extratordedados.com.br' },
  { folder: 'facebook-ads-bot', type: 'Bot/Automação', stack: 'Node.js', description: 'Bot de automação de Facebook Ads' },
  { folder: 'feradoprompt', type: 'SaaS', stack: '.NET API + Next.js + Mobile + n8n', description: 'FeraPrompt — plataforma de prompt management. Full-stack com app mobile e automações n8n' },
  { folder: 'free-entertainment-dashboard', type: 'Ferramenta', stack: 'HTML puro', description: 'Dashboard HTML único para entretenimento gratuito (streaming, jogos, etc.)' },
  { folder: 'hit-digital', type: 'Projeto', stack: 'Angular 18', description: 'Frontend de agência digital HitDigital' },
  { folder: 'instagram-alexandrequeirozmd', type: 'Pipeline', stack: 'Python', description: '[LEGADO — substituído pelo content-engine] Pipeline IG @alexandrequeirozmd. Task Scheduler a migrar.' },
  { folder: 'instagram-bot', type: 'Bot/Automação', stack: 'Python + Selenium', description: 'Bot de automação do Instagram (likes, follows, etc.)' },
  { folder: 'investiq', type: 'SaaS', stack: 'Python + FastAPI + pgvector + Redis', description: 'Motor de decisão financeira para o mercado BR. Ingere notícias + B3 + BCB → AI score → REST API + MCP' },
  { folder: 'kronoos', type: 'Projeto', stack: 'React CRA + API', description: 'App web com módulo de tasks/API. Uso interno' },
  { folder: 'linkedin-job-scraper', type: 'Scraper', stack: 'Python + Playwright', description: 'Scraper de vagas do LinkedIn. Extrai listings com login automatizado' },
  { folder: 'linkedin-scraper', type: 'Scraper', stack: 'Python', description: 'Biblioteca Python para scraping de perfis e dados do LinkedIn' },
  { folder: 'monitor-diario-viagem-6-meses-celina', type: 'Placeholder', stack: '—', description: 'Pasta vazia — monitor de viagem da Celina (6 meses). Não implementado' },
  { folder: 'monitor-vagas', type: 'Bot/Automação', stack: 'Python + SQLite + Telegram', description: 'Monitor de vagas de emprego. Bot Telegram que notifica novas vagas. Tem busca retroativa e filtros' },
  { folder: 'morning-briefing', type: 'Pipeline', stack: 'Python', description: 'Gerador de briefing matinal automatizado. Daily digest com dados relevantes' },
  { folder: 'notebook-llm', type: 'Conteúdo', stack: '—', description: 'Pasta de sources/documentos para uso no Google NotebookLM' },
  { folder: 'paperdrop', type: 'Projeto', stack: 'React Native', description: 'Jogo mobile Flappy Bird implementado com Matter.js. Em desenvolvimento' },
  { folder: 'partiu-rock', type: 'SaaS', stack: 'Ionic + React + Mobile', description: 'PartiuRock — app de eventos musicais. Domínio partiurock.com.br, SSL, Android assetlinks' },
  { folder: 'pipeline-investimentos', type: 'Pipeline', stack: 'Python + Docker + Celery + Alembic', description: 'Pipeline de investimentos com scheduler Celery. Alimenta o InvestIQ' },
  { folder: 'post-creator', type: 'Ferramenta', stack: 'Python + Skills', description: 'Lab de engenharia reversa de automação de conteúdo em escala. Testa ferramentas de criação de posts' },
  { folder: 'procura-apartamento', type: 'Placeholder', stack: '—', description: 'Pasta vazia — ferramenta de busca de apartamentos. Não implementada' },
  { folder: 'saas-lgpd', type: 'Placeholder', stack: '—', description: 'Pasta vazia — SaaS de compliance LGPD. Não implementado' },
  { folder: 'shopee-scraper', type: 'Scraper', stack: 'Python', description: 'Scraper de produtos da Shopee. Um único script Python' },
  { folder: 'social-scripts', type: 'Scripts', stack: 'Python', description: 'Scripts de postagem em redes sociais. LinkedIn poster + post_all.py multi-plataforma' },
  { folder: 'tarefista', type: 'SaaS', stack: '.NET + React', description: 'Tarefista — app de gestão de tarefas. Domínio tarefista.com.br, web + mobile' },
  { folder: 'vaganagringa', type: 'SaaS', stack: '.NET (VagaNaGringa.sln) + React + Mobile', description: 'Plataforma VagaNaGringa principal. Full-stack com e2e, integração social (FB/LinkedIn)' },
  { folder: 'vaganagringa-content-pipeline', type: 'Pipeline', stack: 'Python', description: '[LEGADO — funcionalidades migradas para content-engine 2026-05-14] Pipeline VNG com Kling/Veo3/TTS/YouTube/TikTok.' },
  { folder: 'vaganagringa-git', type: 'Projeto', stack: '.NET + React + Mobile', description: 'Mirror/worktree alternativo do VagaNaGringa com AGENTS.md e ROADMAP' },
  { folder: 'waha-hub', type: 'Infraestrutura', stack: 'Docker + Traefik', description: 'Hub WAHA (WhatsApp HTTP API). Self-hosted WhatsApp API com roteador Traefik' },
  { folder: 'website-aq', type: 'Projeto', stack: 'PHP + Bootstrap/jQuery', description: 'Site pessoal de Alexandre Queiroz (alexandrequeiroz.com.br). PHP legado' },
  { folder: 'whatsapp-marketing', type: 'Placeholder', stack: '—', description: 'Pasta vazia — ferramenta de WhatsApp marketing. Não implementada' },
  { folder: 'whatsapp-scrapper', type: 'Scraper', stack: 'Python + Flask', description: 'Scraper de contatos do WhatsApp. Flask API + runner' },
  { folder: 'whatsapp-sender', type: 'Bot/Automação', stack: 'Python', description: 'Sender de mensagens WhatsApp em massa. Tem rastreamento de contatos enviados' },
];
