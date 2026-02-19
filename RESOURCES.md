# 📚 Recursos Disponíveis - DIAX CRM

Este documento lista todos os serviços, APIs e ferramentas disponíveis para desenvolvimento, automação e deploy. **Consulte aqui antes de sugerir novas implementações.**

---

## 🏗️ Infraestrutura & Hospedagem

| Recurso | Descrição | Status | Uso Recomendado |
|---------|-----------|--------|-----------------|
| **SmarterASP** | Hospedagem Windows com suporte .NET Core/Node | ✅ Produção | APIs .NET 8, SQL Server, Deploy em produção |
| **HostGator Revanda** | Múltiplos sites WordPress, WHM, cPanels, SSL | ✅ Ativo | Hospedagem clientes WordPress, SSL automático |
| **VPS Hostinger n8n** | Servidor para automações avançadas com IA | ✅ Ativo | Workflows n8n, APIs externas, IA avançada |
| **Firebase** | Autenticação, banco realtime, storage, notificações | 🔄 Opcional | Apps mobile, autenticação social, realtime |
| **AWS Free Tier** | Infraestrutura cloud gratuita, pode expandir | 🔄 Opcional | Estudo, experimentação, backups adicionais |
| **Google Cloud Free Tier** | GCP para infraestrutura, bancos, IA | 🔄 Opcional | Estudo, experimentação, backup |
| **SQL Server SmarterASP** | Banco relacional em produção | ✅ Produção | BD principal do DIAX CRM (Servidor: sql1002.site4now.net) |

---

## 🤖 Modelos de IA & LLMs

| Recurso | Tipo | Custo | Status | Melhor Para |
|---------|------|-------|--------|-----------|
| **OpenAI API** | GPT-4, GPT-4o, Vision, Embeddings | 💰 Pago | ✅ Integrada | Primária em produção, IA humanize, prompts |
| **Anthropic Claude** | Claude 3.7 Sonnet | 💰 Pago | 🔄 Disponível | Análise contextual, documentação, RFP |
| **OpenRouter** | Múltiplos modelos (fallback) | 💰 Pago (baixo) | ✅ Disponível | Custo reduzido, múltiplas opções, fallback |
| **Perplexity API** | Busca inteligente + respostas estruturadas | 💰 Pago | ✅ Disponível | Pesquisa estruturada, RAG, research |
| **Deepseek API** | LLM econômico, boa performance | 💰 Pago (muito baixo) | ✅ Disponível | Tarefas variadas, custo otimizado |
| **Google Gemini Free** | Multimodal gratuito | 🆓 Gratuito | ✅ Disponível | Testes, prototipagem, vision tasks |
| **Grok** | IA gratuita, raciocínio rápido | 🆓 Gratuito | ✅ Disponível | Consultas rápidas, bom custo-benefício |
| **Wavespeed AI** | Geração texto rápida/econômica | 💰 Pago (baixo) | ✅ Disponível | Geração texto em massa, automações |
| **HuggingFace** | Modelos abertos, local inference | 🆓 Gratuito | 🔄 Disponível | IA local, modelos customizados, embeddings |

---

## 🎨 Design, Mídia & Automação Criativa

| Recurso | Descrição | Status | Uso Recomendado |
|---------|-----------|--------|-----------------|
| **Adobe Creative Cloud** | Photoshop, Illustrator, Premiere, After Effects | ✅ Ativo | Design, branding, vídeos, identidade visual |
| **Eleven Labs** | Geração voz realista com 100+ vozes | ✅ Ativo | Podcasts, vídeos, automações de áudio |
| **RunwayML** | Criação vídeos com IA, edição inteligente | ✅ Ativo | Criação vídeos, efeitos visuais, deepfakes |
| **Fal AI** | Modelos imagem e vídeo custo reduzido | ✅ Ativo | Geração imagens rápidas, vídeos econômicos |

---

## 🔌 Automação & Integrações

| Recurso | Descrição | Capacidade | Status |
|---------|-----------|-----------|--------|
| **n8n (VPS Hostinger)** | Motor automações visual com IA agents | 2700+ integrações, AI tools nativos | ✅ Produção |
| **Make (Integromat)** | Automações visuais avançadas/alternativa n8n | Automações complexas, webhooks | 🔄 Disponível |
| **Google APIs** | Maps, Autenticação, YouTube, Drive, Vision | Múltiplas functions Google | ✅ Ativo |
| **LinkedIn API** | Vagas, perfis, posts, dados profissional | Scraping, bots, publicação | ✅ Disponível |
| **TwitterX API** | Scraping, bots, automações, publicação | Posts, replies, DMs, analytics | ✅ Disponível |

---

## 💻 IDEs & Ferramentas Dev

| Recurso | Descrição | Status | Melhor Para |
|---------|-----------|--------|-----------|
| **GitHub Copilot** | Sugestões código IA em VSCode/JetBrains | ✅ Ativo | Escrita código rápida, refactoring |
| **Cursor PRO** | IDE com IA integrada, VSCode fork | ✅ Ativo | Velocidade, prototyping, pair programming |
| **Visual Studio Enterprise** | IDE completa .NET, testes, arquitetura | ✅ Ativo | Desenvolvimento DIAX backend |
| **GitHub Pro** | Repositórios privados, CI/CD avançado | ✅ Ativo | Automações, secrets, workflows |
| **Replit** | Ambiente online com IA para rodar projetos | 🔄 Disponível | Prototipagem rápida, demo online |

---

## 📚 Educação Contínua

| Recurso | Tipo | Status | Melhor Para |
|---------|------|--------|-----------|
| **Pluralsight** | Cursos técnicos avançados | ✅ Ativo | Arquitetura, padrões design, .NET avançado |
| **LinkedIn Learning** | Prático, negócios + tecnologia | ✅ Ativo | Soft skills, atualizações, liderança |
| **Udemy** | Cursos avulsos variados em tecnologias | 🔄 Disponível | Tecnologias específicas, atualizações rápidas |

---

## 🔑 API Keys & Variáveis de Ambiente

Adicione ao arquivo `.env.local` ou GitHub Secrets (nunca commitar no repo):

```powershell
# IA - LLM & NLP
$env:OPENAI_API_KEY="sk-..."
$env:ANTHROPIC_API_KEY="sk-ant-..."
$env:OPENROUTER_API_KEY="sk-or-..."
$env:PERPLEXITY_API_KEY="..."
$env:DEEPSEEK_API_KEY="..."
$env:GROK_API_KEY="..."

# Google Services
$env:GOOGLE_API_KEY="..."
$env:GOOGLE_CLIENT_ID="..."
$env:GOOGLE_CLIENT_SECRET="..."

# LinkedIn & Social
$env:LINKEDIN_API_KEY="..."
$env:TWITTER_BEARER_TOKEN="..."

# Media & Audio
$env:ELEVENLABS_API_KEY="..."
$env:FAL_AI_API_KEY="..."
$env:RUNWAY_API_KEY="..."

# Automação n8n
$env:N8N_WEBHOOK_URL="https://vps-hostinger.example/webhook"
$env:N8N_API_KEY="..."

# Infraestrutura
$env:SMARTERASP_FTP_PASSWORD="..."
$env:HOSTGATOR_FTP_PASSWORD="..."
$env:SQL_CONNECTION_STRING="Server=sql1002.site4now.net;..."
```

---

## 📋 Mapeamento: Recurso → Use Case

### Preciso humanizar texto gerado por IA
→ **OpenAI API** (integrada em `Diax.Application/Services/AiHumanizeText`)

### Preciso gerar prompts dinamicamente
→ **OpenAI API** (serviço em `PromptGenerator.cs`)

### Preciso fazer scraping/extração de HTML
→ **Perplexity API** ou **HtmlExtraction** backend

### Preciso automatizar fluxo de trabalho
→ **n8n (VPS Hostinger)** + IA agents

### Preciso integrar com LinkedIn/X
→ **LinkedIn API** ou **TwitterX API** →n8n

### Preciso criar vídeos com IA
→ **RunwayML** ou **Fal AI**

### Preciso sintetizar voz
→ **Eleven Labs API** (integrar em n8n)

### Preciso processamento visão
→ **OpenAI Vision** ou **Google Vision API**

### Preciso de fallback em custo
→ **Deepseek API** ou **OpenRouter**

### Preciso implementar rápido
→ **Grok** (gratuito) ou **Gemini Free** (prototipo)

---

## 💸 Estimativa de Custo Mensal

| Serviço | Uso Estimado | Custo |
|---------|--------------|-------|
| OpenAI API | 10k requests/mês | ~$50-100 |
| Perplexity API | 500 queries/mês | ~$10-20 |
| Deepseek API | 5k requests/mês | ~$5-10 |
| Eleven Labs | 100k caracteres | ~$10-50 |
| Fal AI | 1k imagens/mês | ~$20-50 |
| Google APIs | Maps + Vision + Drive | ~$10-30 |
| Hostinger VPS n8n | Servidor dedicado | ~$20-40 |
| **TOTAL ESTIMADO** | | ~$125-300 |

---

## ✅ Checklist de Setup

- [ ] Adicionar API Keys no GitHub Secrets (para CI/CD)
- [ ] Documentar qual resource usar por feature
- [ ] Revisar quotas de uso API (fim de mês)
- [ ] Configurar alerts de costs em cada serviço
- [ ] Validar backups em AWS/Google Cloud
- [ ] Testar failover entre LLMs (OpenRouter)

---

## 📞 Como Usar Este Documento

1. **Quando pedir feature com IA:** Mencione "veja RESOURCES.md" se tiver dúvida
2. **Quando precisar integração:** Procure em "Automação & Integrações"
3. **Quando tiver que escolher:** Considere custo + performance + disponibilidade
4. **Dúvida sobre um recurso?** Pergunta específica + contexto = resposta melhor

---

**Última atualização:** 19/02/2026
**Responsável:** DIAX CRM Dev Team
