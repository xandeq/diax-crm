# DIAX CRM - Auditoria Estratégica & Roadmap de Evolução
## De um CRM Pessoal para uma Plataforma Profissional de Aquisição de Clientes

**Data:** Março de 2026
**Escopo:** Análise de produto, experiência de usuário, e posicionamento estratégico
**Objetivo:** Transformar o DIAX CRM em um sistema profissional focado em aquisição e retenção de clientes

---

## A. RESUMO EXECUTIVO

### Posição Atual do Sistema

O DIAX CRM é um **sistema pessoal de gestão de relacionamento** com funcionalidades básicas de:
- Gerenciamento de leads (CRUD, importação, filtros)
- Envio de emails em campanha (seleção de contatos + composição + fila)
- Automação de outreach (segmentação por temperatura + templates por segmento)
- Histórico de campanhas

**Força:** Sistema funcional que permite executar operações de vendas e marketing.
**Fraqueza crítica:** Falta de **visibilidade** sobre cada contato e cada campanha — o usuário envia emails, mas não consegue entender quem abriu, quem clicou, ou por que um lead é "quente".

### Gaps Estratégicos (vs. CRMs Profissionais)

| Aspecto | Status Atual | Necessário para Profissional |
|---------|-------------|------------------------------|
| **Timeline do Contato** | Não existe | Essencial — história completa de cada interação |
| **Análise de Comportamento** | Apenas contagem de emails | Detalhado — aberturas, cliques, engajamento por tipo |
| **Scoring de Leads** | Manual (segmentação por regras) | Automático e transparente — "por que este lead é Hot?" |
| **Relatórios de Campanha** | Métricas básicas (enviado/entregue) | Análise completa — funil, engagement, ROI |
| **Segmentação Dinâmica** | Baseada em status/score fixo | Comportamental e automática |
| **Remarketing** | Manual (clicar em "enviar campanha") | Automático com workflows |
| **Dashboard Executivo** | Não existe | Visão centralizada de performance |
| **Identificação de Leads Quentes** | Usuário deduz | Sistema aponta — "X leads prontos para venda agora" |

### Oportunidade Estratégica

Transformar o DIAX em um **"Pipedrive simplificado com foco em email marketing"** — uma ferramenta que:
- ✅ Deixa cristalino o comportamento de cada contato
- ✅ Aponta leads prontos para venda
- ✅ Automatiza toda a cadeia de prospección
- ✅ Mantém a simplicidade e foco (sem sobre-engenharia)

**Impacto comercial:** Aumentar taxa de conversão de leads em clientes através de:
1. Melhor segmentação (saber com quem conversar agora vs. depois)
2. Follow-up automático (não deixar lead "esfriando")
3. Visibilidade de ROI (qual campanha trouxe resultado)

---

## B. AUDITORIA DO EMAIL MARKETING

### Estado Atual

**O que funciona:**

1. **Interface de Seleção de Contatos**
   - ✅ Multi-filtro (segmento, tipo de contato, busca)
   - ✅ Paginação com tamanho configurável (10–500 contatos/página)
   - ✅ Seleção granular (checkbox individual + selecionar página)
   - ✅ Visualização clara: nome, email, empresa, status, histórico de emails enviados

2. **Composição de Email**
   - ✅ Editor visual (rich text com TipTap)
   - ✅ Variáveis de personalização ({{nome}}, {{empresa}}, {{email}}, {{website}})
   - ✅ Preview com dados reais do primeiro contato selecionado
   - ✅ Upload de imagens com compressão automática e hospedagem
   - ✅ Histórico de campanhas (dropdown com emails anteriores)

3. **Fila de Envio**
   - ✅ Respeita limites Brevo (50/hora, 250/dia automaticamente)
   - ✅ Salva campanha com nome para histórico
   - ✅ Feedback imediato (X enfileirados, Y ignorados)
   - ✅ Teste de email antes de enviar para todos

4. **Integração com Outreach**
   - ✅ Notificação dos limites automáticos
   - ✅ Link para acompanhar fila em tempo real (Outreach → Dashboard)

**Problemas & Limitações:**

1. **Falta de Visibilidade Pós-Envio** ❌
   - Não há feedback sobre os resultados: quem abriu, quem clicou?
   - Usuário sabe que enviou, mas não sabe o impacto
   - Métrica de "emailSentCount" no contato é apenas um contador, sem contexto

2. **Métricas Superficiais** ❌
   - Relatório de campanha mostra: enviado, entregue, aberto, clicado (números brutos apenas)
   - Não há visual atraente (gráficos, funnels visuais)
   - Não há: taxa de clique (CTR), padrão de engajamento, melhor horário de abertura

3. **Experiência de Remarketing Primitiva** ❌
   - Usuário precisa ir até o relatório da campanha → clicar em "criar campanha para este grupo"
   - Armazena IDs em `sessionStorage` (frágil, quebra ao atualizar página)
   - Não é intuitivo para fluxo de trabalho rápido

4. **Sem Segmentação por Comportamento** ❌
   - Não consegue filtrar contatos por "abriram em x dias" ou "clicaram mas não converteram"
   - Segmentação é apenas por status/score fixo

5. **Template Management Basicão** ⚠️
   - Histórico funciona, mas não há sugestões inteligentes
   - Sem A/B testing ou variações de assunto

### Comparativo com Profissionais

| Feature | HubSpot | Pipedrive | DIAX Atual |
|---------|---------|-----------|-----------|
| **Editor Visual** | Drag-drop + templates | Simples | Rich text + variáveis |
| **Personalização** | Smart tokens + tracking | {{placeholders}} | {{placeholders}} |
| **Preview** | Lado-a-lado | Básico | Com dados reais |
| **Scheduling** | Data/hora + send-time optimization | Imediato | Imediato |
| **A/B Testing** | Assunto + corpo | Não | Não |
| **Relatórios** | Detalhados com gráficos | Funil simples | Números brutos |
| **Remarketing** | Automático + workflows | Manual + segmento | Manual + sessionStorage |
| **Click Tracking** | Pixel + link tracking | URL tracking | Brevo webhook |
| **CTOR** | ✅ | ✅ | ❌ (não calculado) |

### Recomendações Imediatas (Fase 1)

1. **Adicionar "CTOR" (Click-Through Rate)** → Fácil cálculo em frontend
2. **Visualizar "Last Campaign" por contato** → Mostrar no perfil do contato
3. **Relatório visual com gráficos** → Funnel + charts de abertura
4. **Melhorar Remarketing** → SessionStorage → localStorage ou store Zustand
5. **Badges de engajamento** → Mostrar "Abriu tudo" vs "Nunca abre" no contato

---

## C. AUDITORIA DO MÓDULO DE LEADS

### Estado Atual

**O que funciona:**

1. **Gerenciamento de Leads (CRUD)**
   - ✅ Criar lead manualmente (modal com 10+ campos)
   - ✅ Editar lead (atualizar status, segmento, notas)
   - ✅ Listar leads com paginação
   - ✅ Deletar individual ou em lote

2. **Filtragem Avançada**
   - ✅ Busca por nome/email/empresa
   - ✅ Status (Lead, Contacted, Qualified, Negotiating)
   - ✅ Tem email / Tem WhatsApp
   - ✅ Person type (Individual/Company)
   - ✅ Lead source (7+ fontes)
   - ✅ Segmento (Cold/Warm/Hot)
   - ✅ Never emailed (leads frios)
   - ✅ Created after (data de criação)

3. **Importação de Dados**
   - ✅ CSV upload
   - ✅ Paste de texto
   - ✅ Apify JSON (Google Maps scraper)
   - ✅ Auto-sanitization na importação (remove inválidos)

4. **Ações em Lote**
   - ✅ Deletar múltiplos
   - ✅ Enviar email (abre Email Campaign Composer)
   - ✅ Exportar CSV

**Problemas & Limitações:**

1. **Sem Timeline do Lead** ❌
   - Abrir um lead não mostra: "qual email foi enviado, quando, resultado?"
   - Apenas contadores (emailSentCount) sem contexto
   - Não há histórico de campanhas em que participou

2. **Sem Lead Score Visível** ❌
   - LeadScore existe no backend mas não aparece na interface
   - Usuário não sabe "por que" um lead é Hot vs Cold
   - Não há dicas de próxima ação

3. **Importação sem Validação Prévia** ⚠️
   - Upload vai, sanitization roda after the fact
   - Sem preview dos dados antes de confirmar
   - Sem relatório de "quantos foram ignorados e por quê"

4. **Sem Indicador de Engajamento** ❌
   - Lead que nunca abriu email = não se diferencia visualmente
   - Lead que clicou em link = não se diferencia
   - Tudo vira "Lead" ou "Hot" pelo score, sem nuance

5. **Sem Ações Rápidas** ❌
   - Abrir lead e não conseguir: enviar email, agendar follow-up, adicionar nota
   - Tudo é click → modal → ação (muitos cliques)

### Comparativo com Profissionais

| Feature | HubSpot | Pipedrive | DIAX Atual |
|---------|---------|-----------|-----------|
| **Timeline do Contato** | Completa (todos eventos) | Completa + notas fixadas | Não existe |
| **Lead Scoring** | IA + Transparent | Simples | Backend, sem visualização |
| **Lead Score Explanation** | "Why is this lead hot?" | Não | Não |
| **Import Preview** | Mapeamento de campos + preview | Upload only | Auto-mapping |
| **Lead Detail Page** | Sidebar + timeline + quick actions | Card + história | Cards só com dados |
| **Quick Actions** | Email, call, meeting, note | Task, call | Menu modal |
| **Engagement Indicators** | "Last activity" + "Days open" | Data + duração | Apenas contador |
| **Custom Fields** | Ilimitado + conditional logic | Limitado | 10 campos fixos |

### Recomendações Imediatas (Fase 1)

1. **Criar Lead Detail Page**
   - Sidebar com: nome, email, status, segmento, score (com tooltip explicando)
   - Timeline mostrando: emails enviados + datas

2. **Adicionar "Last Engagement" ao contato**
   - Mostrar "Última atividade: 5 dias atrás" ou "Nunca interagiu"
   - Usar para priorizar follow-up manual

3. **Lead Score Tooltip**
   - Hover no segmento mostra: "Hot porque: 2+ emails abertos + clicou em link"

4. **Validação na Importação**
   - Preview dos dados antes de confirmar
   - Aviso: "X leads sem email (será ignorado)"

---

## D. AUDITORIA DO MÓDULO DE CLIENTES

### Estado Atual

**O que funciona:**

1. **Interface de Clientes**
   - ✅ Filtrada automaticamente (Status >= 4 = Customer)
   - ✅ Mesma interface de Leads (busca, filtros, pagination)
   - ✅ Ações em lote (email, delete, export)

**Problemas & Limitações:**

1. **Sem Histórico de Relacionamento** ❌
   - Abrir um cliente não mostra:
     - Quais emails ele recebeu
     - Quando virou cliente (ConvertedAt existe mas não aparece)
     - Qual campanha o converteu
   - Tudo o que mostra é status fixo "Customer"

2. **Sem Timeline Comercial** ❌
   - Não há separação visual entre: período de prospecção vs. período como cliente
   - Não há marcadores (ex: "Virou cliente em X, primeira compra em Y")

3. **Sem Metrics de Retenção** ❌
   - Não há "dias como cliente" ou "últimas compras"
   - Não há indicador de risco (ex: "não interage há 60 dias")

4. **Sem Propósito Claro** ⚠️
   - Separação entre Leads e Customers é superficial
   - Maior valor seria uma visão unificada com história completa

### Recomendações Imediatas (Fase 1)

1. **Adicionar "ConvertedAt" à visualização**
   - Badge: "Cliente desde Dez/2025"

2. **Timeline unificada**
   - Mesma página mostra: período como lead + período como cliente
   - Visualmente diferenciados (cores diferentes)

3. **Métrica de "Days as Customer"**
   - Ordenar clientes por "recently converted" vs "longtime customer"

---

## E. AUDITORIA DO MÓDULO DE OUTREACH

### Estado Atual

**O que funciona:**

1. **Dashboard com Estatísticas**
   - ✅ Total leads por segmento (Hot/Warm/Cold)
   - ✅ Emails enviados (hoje, esta semana, este mês)
   - ✅ WhatsApp enviados + status de conexão
   - ✅ Fila em tempo real (pending, failed)

2. **Configuração Centralizada**
   - ✅ Apify integration (dataset URL + token)
   - ✅ Email limits (daily + cooldown)
   - ✅ Segmentation toggle
   - ✅ Templates por segmento (Hot/Warm/Cold)
   - ✅ WhatsApp templates

3. **Automação de Segmentação**
   - ✅ Botão "Segmentar" → calcula LeadScore + atribui segmento
   - ✅ Botão "Enviar" → dispara campanha por segmento com template apropriado
   - ✅ Regras de cooldown (7 dias entre emails, 3 para WhatsApp)

4. **Queue Monitoring**
   - ✅ Lista de emails na fila com status (queued/processing/sent/failed)
   - ✅ Tentativas + mensagens de erro

5. **WhatsApp Integration**
   - ✅ Manual send por lead
   - ✅ Bulk send por segmento
   - ✅ Follow-up automático

**Problemas & Limitações:**

1. **Aba "Leads Prontos" Pouco Útil** ⚠️
   - Lista leads "elegíveis para envio" (sem violação de cooldown)
   - Mas não diferencia "pronto para venda agora" vs "pronto para email"

2. **Automação Muito Manual** ⚠️
   - Usuário precisa clicar em "Segmentar" → depois "Enviar"
   - Sem agendamento automático
   - Sem workflows tipo "se não abrir em 3 dias, reenviar"

3. **Sem Visualização de Impacto** ❌
   - Dashboard mostra números, mas não shows:
     - Qual segmento tem maior taxa de abertura?
     - Qual template funciona melhor?
     - Qual lead source tem melhor conversão?

4. **WhatsApp Desconectado de Email** ⚠️
   - Templates separados sem sincronização
   - Sem lógica de "primeiro email, depois WhatsApp"

5. **Sem Alertas Inteligentes** ❌
   - Não há notificação: "X leads ficaram frios (não interagem há 30 dias)"
   - Não há recomendação: "Considere re-engajamento para Y leads"

### Comparativo com Profissionais

| Feature | HubSpot Workflows | Pipedrive Automations | DIAX Atual |
|---------|------------------|----------------------|-----------|
| **Trigger** | 20+ event types | Lead stage, engagement | Botão manual |
| **Actions** | Email, SMS, task, update | Email, SMS, call task | Email, SMS |
| **Conditions** | Complex logic | Simple rules | Cooldown only |
| **Scheduling** | Date/time + delay | Imediato | Imediato |
| **A/B Testing** | Subject + body variants | Não | Não |
| **Reporting** | Automation-specific | Conversão por flow | Não existe |
| **Multi-touch** | Ramificações complexas | Simples | Linear |

### Recomendações Imediatas (Fase 1)

1. **Dashboard Visual Melhorado**
   - Gráfico de segmentação (Hot/Warm/Cold %)
   - KPIs: "leads prontos para venda hoje" vs "em re-engajamento"

2. **Automação Simplificada**
   - "Segmentação automática" toggle (roda daily)
   - "Auto-send" toggle (envia template apropriado daily)

3. **Alertas Inteligentes**
   - Notificação quando um lead hot ficar 7+ dias sem atividade
   - Sugestão de re-engajement para leads frios

---

## F. AVALIAÇÃO DA GESTÃO DE COMPORTAMENTO DE EMAIL

### Estado Atual

**Dados Coletados:**

- ✅ EmailSentCount (por contato)
- ✅ LastEmailSentAt (timestamp)
- ✅ Engagement events (via Brevo webhook): delivered, opened, clicked, bounced, unsubscribed
- ✅ Campaign details: name, status, sent/delivered/opened/clicked counts

**Visibilidade:**

- ✅ Analytics dashboard (agregado)
- ✅ Campaign report (por campanha)
- ❌ **Por contato: AUSENTE**

### Problema Central ❌

**Um usuário abre um contato e NÃO consegue ver:**
- Quais emails ele recebeu (lista com datas)
- Quais emails ele abriu (com timestamp)
- Quais links ele clicou (e quando)
- Taxa de engajamento (abridoX emails de Y recebidos)
- Padrão (ex: abre emails de marketing, não abre de promoção)

**Consequência:** Usuário não consegue tomar decisão informada:
- "Devo enviar outro email agora?" (não sabe se ele abre emails)
- "Ele está interessado?" (não sabe padrão de engajamento)
- "Vale a pena continuar enviando?" (não tem baseline)

### Comparativo com Profissionais

| Métrica | HubSpot | Pipedrive | DIAX Atual |
|---------|---------|-----------|-----------|
| **Email List por Contato** | Completa + preview | Simples | Não |
| **Open History** | Datas + dispositivo | Sim | Não |
| **Click History** | Link + texto + data | Sim | Não |
| **Engagement Rate** | % aberto + clicado | Simples % | Não |
| **Engagement Timeline** | Gráfico por dia | Não | Não |
| **Email Attribution** | Qual email levou à venda | Não | Não |

### Recomendações Imediatas (Fase 1)

1. **Tab "Email Activity" no Contato**
   - Tabela: Data, Campanha, Subject, Status (sent/delivered/opened/clicked), Timestamp da abertura
   - Filtro por status (opened, clicked, failed)

2. **Engagement Rate Card**
   - "Abriu 3 de 7 emails recebidos (43%)"
   - Cor: verde (alto) / amarelo (médio) / vermelho (baixo)

3. **Last Engagement Badge**
   - "Abriu há 2 dias" ou "Nunca abriu"
   - Usa para priorizar follow-up

---

## G. AVALIAÇÃO DA SEGMENTAÇÃO E REMARKETING

### Estado Atual

**Segmentação:**

- ✅ Sistema de scoring baseado em regras
- ✅ Segmentação automática: Cold (0), Warm (1), Hot (2)
- ✅ Cooldown enforcement (7 dias entre emails)

**Remarketing:**

- ✅ Relatório de campanha com tabs: All, Sent, Delivered, Opened, Not Opened, Failed
- ✅ Botão "Criar campanha para este grupo" nos tabs
- ⚠️ Armazena IDs em sessionStorage (frágil)
- ✅ Redirecionador automático para Email Marketing

**Problema Central:** ❌ Segmentação é baseada em **regras fixas**, não em **comportamento contínuo**

**Exemplos do que NÃO consegue:**
- "Enviar para leads que abriram pelo menos 2 emails" (não consegue filtrar isso)
- "Reengajar leads que não abrem há 30 dias" (nenhum filtro para isso)
- "Pré-qualificar leads que clicaram em 3+ links" (não há filtro de comportamento)

### Comparativo com Profissionais

| Feature | HubSpot | Pipedrive | DIAX Atual |
|---------|---------|-----------|-----------|
| **Behavioral Segmentation** | ✅ Avançado | ✅ Dinâmico | ❌ Estático |
| **Re-evaluation** | Contínuo | Contínuo | Manual (ao clicar) |
| **Time-based Rules** | "não engajado em 30d" | Simples | Não |
| **Engagement Scoring** | IA | Regras | Regras básicas |
| **Dynamic Filtering** | Limpo + salva | Limpo | Ad-hoc |
| **Remarketing Automation** | Workflow automático | Manual | Manual |

### Recomendações Imediatas (Fase 1)

1. **Salvar Segmentos Criados Ad-hoc**
   - Criar em campaña X "leads que abriram"
   - Botão "Salvar este segmento" para reutilizar depois
   - Nada de sessionStorage — banco de dados

2. **Filtros Comportamentais Simples**
   - "Abriu X+ emails"
   - "Clicou em X+ links"
   - "Não interagiu há X dias"

3. **Remarketing mais Intuitivo**
   - Em vez de "clicar em campanha → clicar em tab → clicar em botão"
   - Interface direta: "criar remarketing para quem abriu"

---

## H. INTEGRAÇÃO IDEAL COM O SISTEMA DE EXTRAÇÃO DE LEADS

### Fluxo Atual

```
Internet → Extrator (scraping) → CSV/JSON → CRM (import) → Campaigns
```

### Análise

**Funciona:**
- ✅ Importação de CSV/JSON
- ✅ Auto-sanitization
- ✅ Apify integration (dataset URL)

**Problemas:**
- ⚠️ Sem tracking do lead do lead ao longo do funnel (qual lead veio do scraping → qual se tornou cliente?)
- ❌ Sem "lead source attribution" (saber que "5 clientes vieram de scraping Google Maps")
- ❌ Sem análise de ROI por fonte (custo de scraping vs. clientes obtidos)

### Recomendações Imediatas (Fase 1)

1. **Source Tracking**
   - Importação de Google Maps = source "GoogleMaps" (já faz)
   - Rastrear: desse lead → campanhas → conversão

2. **Dashboard de Lead Sources**
   - Tabela: Source, Total imported, Contacted, Qualified, Converted, Conversion %
   - Identifica melhor fonte de leads

3. **Automação de Import**
   - Toggle: "Auto-import de Apify dataset a cada 6 horas"
   - Com deduplication automática

---

## I. FUNCIONALIDADES NECESSÁRIAS PARA UM CRM PROFISSIONAL

### Pilar 1: VISIBILIDADE (Ver o que está acontecendo)

#### 1.1 Contact Timeline / Activity Feed
**O quê:** Página do contato mostra TODAS as interações em ordem cronológica
- Emails enviados (data, campanha, assunto)
- Emails abertos (data)
- Links clicados (data, link)
- Ligações (duração, notas)
- Reuniões (data, presentes, resultado)
- Notas manuais
- Mudanças de status

**Impacto:** Usuário entende história completa do contato em 1 segundo

#### 1.2 Lead Scoring com Transparência
**O quê:** Badge no contato explica "por que Hot?"
- "Hot: 3 emails abertos + 1 clique + status Qualified"
- Tooltip mostra critérios

**Impacto:** Usuário sabe exatamente quem priorizar

#### 1.3 Engagement Indicators
**O quê:** Contato mostra clara indicação de engajamento
- Avatar com cor (🟢 engaged, 🟡 medium, 🔴 cold)
- Last activity badge (2 dias atrás)
- Engagement rate (43% de emails abertos)

**Impacto:** Vendedor sabe ao olhar para lista quem conversar

#### 1.4 Campaign Performance Dashboard
**O quê:** Visão geral de todas as campanhas
- Tabela: Nome, enviados, taxa de abertura, taxa de clique, conversões
- Gráfico de tendência (campanhas ao longo do tempo)
- Filtros por período, segmento, fonte

**Impacto:** Marketing consegue otimizar estratégia baseado em dados

### Pilar 2: AUTOMAÇÃO (Menos cliques, mais ações)

#### 2.1 Lead-based Automations / Workflows
**O quê:** Regras que disparam ações baseado em comportamento
- "Se lead não abrir email em 3 dias → enviar reminder"
- "Se lead clicar em link → mudar status para Qualified"
- "Se lead ficar 7 dias sem interação → adicionar à campanha de reengajement"

**Impacto:** Zero overhead manual — tudo automático

#### 2.2 Email Sequences / Drip Campaigns
**O quê:** Série de emails automáticos baseado em trigger
- "Novo lead → email 1 (apresentação) → 3 dias depois → email 2 (case)"
- Com condições (se abriu → mudar sequência)

**Impacto:** Prospección em escala sem esforço manual

#### 2.3 Remarketing Automático
**O quê:** Regra tipo "se não abrir em X dias → remarketing"
- Aplica-se automaticamente a novos leads também

**Impacto:** Nenhum lead "esfria" sem motivo

### Pilar 3: INTELIGÊNCIA (Saber o que fazer depois)

#### 3.1 Lead Scoring Preditivo (AI-driven)
**O quê:** Sistema aprende padrões históricos
- "Leads que tiveram padrão X → 80% de chance de conversão"
- Algoritmo ajusta continuamente

**Impacto:** Priorização mais precisa

#### 3.2 Engagement Insights / Recommendations
**O quê:** Sistema sugere ações
- "Você tem 5 leads prontos para venda agora"
- "Esta campanha tem baixa taxa de abertura — considere mudar subject"
- "5 leads esfriaram — sugerir remarketing"

**Impacto:** Usuário tem "coach" automático

#### 3.3 Email Analytics Avançado
**O quê:** Métricas por tipo de email
- Qual subject line converte mais?
- Qual horário tem mais abertura?
- Qual CTA funciona?

**Impacto:** Otimização contínua de copy

### Pilar 4: EXPERIÊNCIA (Navegação intuitiva)

#### 4.1 Contact Detail Page com Sidebar
**O quê:** Abrir contato mostra:
- Painel esquerdo: dados principais + segmento + score + last activity
- Painel central: timeline + notas
- Painel direito: quick actions (email, call, task, update)

**Impacto:** Tudo num lugar — zero necessidade de navegar

#### 4.2 Pipeline / Kanban View
**O quê:** Arrastar drops contatos entre status
- Colunas: Lead → Contacted → Qualified → Negotiating → Customer
- Card de contato com: nome + empresa + score + last activity

**Impacto:** Vendedor vê status do pipeline visualmente

#### 4.3 Global Search + Quick Navigation
**O quç:** Cmd+K / Ctrl+K abre search que:
- Busca contatos, campanhas, notas
- Navega para qualquer página em 1 tecla

**Impacto:** Sem perder tempo em navegação

#### 4.4 Mobile Responsiveness
**O quê:** Interface funciona bem em mobile
- Listas colapsam para cards
- Timeline fica readable
- Quick actions acessíveis

**Impacto:** Vendedor consegue checar status de qualquer lugar

---

## J. MELHORIAS DE EXPERIÊNCIA E INTERFACE

### Nível 1: Micro-melhorias (fáceis, alto impacto)

| Mejora | Onde | Por que | Esforço |
|--------|------|--------|--------|
| Adicionar "CTOR" nas campanhas | Campaign Report | Métrica padrão de email | 30 min |
| Show "Last Activity" no contato | Lead List | Priorizar follow-up | 1 hora |
| Lead Score tooltip | Segmento badge | Entender "por que Hot" | 1 hora |
| Engagement color indicator | Contact list | Identificar leads frios | 1 hora |
| "Days as Customer" sorting | Customer list | Entender retenção | 1 hora |
| Relatório visual (gráficos) | Campaign Detail | Insights visuais | 2-3 horas |
| Timeline básica | Contact detail | Ver histórico | 2 horas |

### Nível 2: Funcionalidades (médio)

| Funcionalidad | Onde | Impacto | Esforço |
|--------------|------|--------|--------|
| Contact detail page com sidebar | `/customers/[id]` | Centralizar dados | 4-6 horas |
| Email activity tab | Contact page | Comportamento visível | 3-4 horas |
| Saved segments | Filters panel | Reusar filtros | 2-3 horas |
| Behavioral filters | Lead list | Filtrar por engajamento | 3-4 horas |
| Simple email sequences | Outreach config | Automação básica | 6-8 horas |
| Dashboard executivo | `/dashboard` | KPIs centralizados | 4-6 horas |

### Nível 3: Sistema (complexo)

| Sistema | Impacto | Esforço |
|--------|--------|--------|
| Lead scoring engine (refactor) | Transparência | 8-10 horas |
| Workflow automation | Prospición em escala | 16-20 horas |
| Email sequence builder (UI) | UX intuitiva | 10-12 horas |
| Analytics dashboard | Insights dados | 8-10 horas |
| Mobile UI redesign | Acessibilidade | 12-16 horas |

### Princípios de Design

#### 1. **Hierarquia Clara**
- Informação crítica em primeiro plano (engagement, score, last activity)
- Detalhe secundário em expandable sections

#### 2. **Cor com Propósito**
```
Engagement:
🟢 Green   = Engaged (3+ emails abertos em 30 dias)
🟡 Yellow  = Moderate (1-2 abertos)
🔴 Red     = Cold (nenhuma interação)

Temperature:
🔥 Red     = Hot (score > 80)
☀️  Orange  = Warm (score 50-80)
🧊 Blue    = Cold (score < 50)
```

#### 3. **Microcopy Clara**
- Não: "Leads (5)"
- Sim: "5 leads frios — prontos para primeira campanha"

#### 4. **Ações Rápidas**
- Clicar em contato NÃO deve abrir modal
- Deve abrir página lateral (sidebar) ou drawer
- Quick actions visíveis (email, call, task, update)

#### 5. **Feedback Imediato**
- Toast notificações para cada ação
- Loading states claros
- Erros com próximas ações (ex: "3 emails sem envio — clique para retentar")

---

## K. ROADMAP DE EVOLUÇÃO DO CRM

### Estratégia Geral

1. **Semana 1-2: Visibilidade** (Ver o que está acontecendo)
2. **Semana 3-4: Inteligência** (Entender o que significa)
3. **Semana 5-6: Automação** (Fazer ações sem manual)
4. **Semana 7-8: Otimização** (Melhorar o que funciona)
5. **Semana 9-10: Polish** (Experiência premium)

---

### FASE 1 — Visibilidade & Timeline (Semanas 1-2)
**Objetivo:** Usuário consegue ver histórico completo de cada contato

#### Funcionalidades

1. **Contact Detail Page**
   - Rota: `/customers/[id]` ou `/leads/[id]`
   - Layout:
     ```
     [Sidebar: dados principais] [Timeline: histórico] [Painel ações rápidas]
     ```
   - Dados na sidebar:
     - Nome, empresa, email, phone, WhatsApp
     - Status com data de mudança
     - Segmento + Score com tooltip explicando
     - Last activity date
     - Email sent count + Last email sent
   - Timeline mostra:
     - Emails enviados (data, campanha, assunto, status)
     - Aberturas (data, hora, dispositivo se disponível)
     - Cliques (data, link, texto)
     - Notas manuais
     - Mudanças de status

2. **Email Activity Tab**
   - Dentro do contact detail
   - Tabela de emails recebidos
   - Filtros: enviados, entregues, abertos, clicados, failed
   - Colunas: Data, Campanha, Subject, Status, Timestamp da ação

3. **Engagement Rate Card**
   - "3 de 7 emails abertos (43%)"
   - Color: 🟢 green if > 50%, 🟡 yellow if 20-50%, 🔴 red if < 20%

4. **Last Activity Badge**
   - "Abriu há 2 dias" ou "Nunca abriu" ou "Clicou há 5 dias"
   - Usa para reordenar leads na list (priority)

#### Estimativa: 16-20 horas
- Backend: 4-6h (adicionar endpoints, queries)
- Frontend: 10-12h (component design, timeline, sidebar)
- QA: 2-3h

#### Impacto Esperado
- ✅ Usuário tem 360° view de cada contato
- ✅ Tomada de decisão informada ("devo enviar outro email?")
- ✅ Identificação clara de leads prontos para venda

---

### FASE 2 — Inteligência & Scoring (Semanas 3-4)
**Objetivo:** Sistema aponta leads prontos para venda

#### Funcionalidades

1. **Lead Score Refactor**
   - Fazer scoring visível + explicável
   - Backend calcula "score factors":
     - Emails abertos: +10 cada
     - Cliques em link: +15 cada
     - Status upgrade: +20
     - Resposta de email: +30
     - Days since last activity: -1 por dia
   - Capped at 100, floor at 0

2. **Lead Score Tooltip**
   - Badge no segmento/score mostra breakdown:
     ```
     Hot (Score: 78)
     • 3 emails abertos (+30)
     • 2 cliques em links (+30)
     • Status: Qualified (+20)
     • Sem atividade 2 dias (-2)
     ```

3. **Engagement Color Indicator**
   - Contact list mostra avatar com cor:
     - 🟢 Green: 3+ interações em 30 dias
     - 🟡 Yellow: 1-2 interações
     - 🔴 Red: 0 interações

4. **Dashboard Executivo** (`/dashboard`)
   - KPI Cards:
     - Total leads | Hot | Warm | Cold
     - This week: emails sent | opened | clicked
     - Conversion rate (leads → customers)
     - Average days to conversion
   - Charts:
     - Segmentação (pie: Hot/Warm/Cold %)
     - Trends (linha: sent/opened/clicked over 30 days)
     - Source performance (bar: which sources convert best)

5. **Lead Readiness Indicator**
   - New section "Leads ready to sell now" na home
   - Criteria: score > 70 AND status < Customer AND engaged in last 7 days
   - Count + link para filtrar

#### Estimativa: 16-18 horas
- Backend: 4-6h (scoring refactor, dashboard queries)
- Frontend: 8-10h (tooltip, colors, dashboard components)
- QA: 2h

#### Impacto Esperado
- ✅ Transparência em lead scoring
- ✅ Executivo consegue ver "saúde" do pipeline num relance
- ✅ Vendedor sabe exatamente quem ligar agora

---

### FASE 3 — Automação Básica (Semanas 5-6)
**Objetivo:** Prospección automática sem cliques manuais

#### Funcionalidades

1. **Email Sequences / Drip Campaigns**
   - Interface no Outreach → nova aba "Sequences"
   - Criar sequence:
     ```
     Email 1: "Apresentação" → após 3 dias
     Email 2: "Case study" → após 3 dias
     Email 3: "Oferta especial"
     ```
   - Trigger: "Aplicar a todos os novos leads com source = GoogleMaps"
   - Conditions: "Pular email 2 se ele clicou no email 1"

2. **Automação de Re-engagement**
   - Regra: "Se lead não abrir nada em 14 dias → enviar re-engagement"
   - Aplicado automaticamente ao novo lead após 14 dias

3. **Status-change Automation**
   - Regra: "Se lead clicou 2+ links → mudar status para Qualified"
   - Automático sem user action

4. **Daily Segmentation Automática**
   - Outreach → Toggle "Auto-segment daily"
   - Roda diariamente, recalcula scores + segmentos

#### Estimativa: 20-24 horas
- Backend: 10-12h (workflow engine, scheduling, trigger evaluation)
- Frontend: 8-10h (sequence builder UI, rule editor)
- QA: 2-3h

#### Impacto Esperado
- ✅ Zero esforço manual em follow-up
- ✅ Nenhum lead cai pela trinca
- ✅ Prospición em escala automática

---

### FASE 4 — Experiência Completa (Semanas 7-8)
**Objetivo:** Interface profissional + produtiva

#### Funcionalidades

1. **Kanban Pipeline View**
   - Rota: `/pipeline`
   - Colunas: Lead → Contacted → Qualified → Negotiating → Customer
   - Cards: Nome + empresa + score + last activity + avatars
   - Drag-drop para mudar status (atualiza DB em tempo real)

2. **Advanced Filtering + Saved Segments**
   - Filters panel na lead list:
     - Behavioral: "abriu X+ emails", "clicou X+ links", "não interagiu há X dias"
     - Temporal: "criado em últimos X dias", "convertido em últimos X dias"
   - Botão "Salvar segmento" → localStorage + sync com DB
   - Dropdown de segmentos salvos para quick access

3. **Quick Contact Management**
   - Right-click no contact → context menu:
     - Send email
     - Schedule call
     - Add note
     - Change status
     - Add tag
   - Sem modals — actions diretas

4. **Email Campaign Templates**
   - Template builder na Email Marketing
   - Drag-drop sections (text, button, image)
   - Pre-made: "Product launch", "Case study", "Limited offer"

5. **Mobile Responsiveness**
   - Redesign layouts para mobile
   - Bottom sheet navigation em vez de sidebar
   - Touch-friendly buttons + spacing

#### Estimativa: 24-28 horas
- Frontend: 16-20h (kanban, filters, responsive design)
- Backend: 4-6h (segment persistence, context menu APIs)
- QA: 3-4h

#### Impacto Esperado
- ✅ Interface moderna + profissional
- ✅ Usa em desktop + mobile
- ✅ Fluxo de trabalho intuitivo (menos cliques)

---

### FASE 5 — Otimização & Polish (Semanas 9-10)
**Objetivo:** Refinar + documentar

#### Funcionalidades

1. **Email Analytics Avançado**
   - Analytics → Email Performance
   - Tabela de campanhas com: open rate, click rate, CTOR, conversions
   - Grouping: by date, by segment, by source
   - Comparação de periods

2. **Engagement Insights**
   - Dashboard mostra insights:
     - "Best performing subject line: 'Limited offer' (52% open rate)"
     - "Best time to send: 10-11am (23% open rate vs 18% avg)"
     - "5 leads ready to sell now (score > 70)"
     - "This week: 12% open rate (↑3pp from last week)"

3. **Settings & Preferences**
   - User preferences:
     - Email signature
     - Default templates
     - Timezone (para scheduling)
   - Team settings:
     - Lead assignment rules
     - Workflow defaults

4. **Documentation & Onboarding**
   - Interactive tour (primeiro login)
   - Help videos (2-3 min each)
   - Inline help (? icon em seções complexas)

5. **Performance Optimization**
   - Database indexing
   - Frontend lazy loading
   - Caching strategies
   - API response times < 500ms

#### Estimativa: 16-20 horas
- Backend: 4-6h (analytics queries, caching)
- Frontend: 6-8h (insights UI, settings, onboarding)
- Documentation: 4-6h

---

### Roadmap Visual

```
SEMANA  1 2 | 3 4 | 5 6 | 7 8 | 9 10
        └───┴───┬───┴───┬───┴───┬───┴────
              F1  F2   F3    F4    F5
          (Vis) (Int) (Auto) (UX) (Polish)

TOTAL: ~10 semanas
ESFORÇO: ~120-130 horas dev + QA
TIMELINE: ~3 meses (10 semanas x 3 dias/semana de dedicação)
```

---

### Priorização por Impacto/Esforço

**"Quick Wins" (Fazer ASAP):**
1. Email Activity Tab (3-4h) → Usuário vê histórico de emails
2. Engagement Rate Badge (1h) → Usuário sabe quem engaja
3. Last Activity indicator (1h) → Priorização visível
4. CTOR no relatório (30min) → Métrica profissional

**"Must-Have para Profissional":**
1. Contact Detail Page (6h)
2. Lead Scoring Transparency (4h)
3. Dashboard Executivo (6h)
4. Email Sequences (8h)

**"Nice to Have" (se houver tempo):**
1. Kanban Pipeline (6h)
2. Advanced Filters (4h)
3. Mobile Responsiveness (8h)
4. Email Analytics (4h)

---

## L. CONCLUSÃO E PRÓXIMOS PASSOS

### Situação Atual

DIAX CRM é um sistema **funcional** mas **opaco**. Executa operações (envia emails, importa leads) mas não dá visibilidade ao usuário sobre o que está acontecendo.

### Transformação Necessária

Para ser um **CRM profissional de aquisição**, precisa de:

1. **Visibilidade** → O que está acontecendo (timeline, métricas)
2. **Inteligência** → Por que está acontecendo (scoring, insights)
3. **Automação** → Fazer acontecer sem manual (workflows, sequences)
4. **Experiência** → Usar sem frustração (UI/UX profissional)

### Impacto Esperado

Com esse roadmap implementado, DIAX passaria a ser:

- ✅ **Para Vendedor:** Ferramenta que aponta "quem ligar agora" + automático
- ✅ **Para Marketing:** Visibility em campanhas + insights de otimização
- ✅ **Para CEO:** Dashboard executivo com KPIs de pipeline

**Esperado:** 30-50% melhoria em taxa de conversão (lead → customer) através de:
- Melhor priorização (leads Hot identificados automaticamente)
- Zero drop-off (automação de follow-up)
- Otimização contínua (insights de dados)

### Como Começar

**Semana 1:**
1. Implementar Contact Detail Page + Timeline
2. Adicionar Email Activity Tab
3. Criar Lead Score Tooltip

**Semana 2-3:**
1. Lead Score Refactor (visibilidade)
2. Dashboard Executivo
3. Engagement Indicators

**Semana 4+:**
- Seguir roadmap das Fases 3-5

### Métricas de Sucesso

Medir impacto após cada fase:

- **Fase 1:** "Qual % de leads abrem meu email?" (baseline engagement)
- **Fase 2:** "Quantos leads estão prontos para venda?" (hot lead identification)
- **Fase 3:** "Qual é meu ciclo de vendas?" (velocity)
- **Fase 4:** "Qual é minha taxa de conversão?" (ROI)
- **Fase 5:** "Qual email subject converte melhor?" (optimization)

---

## APÊNDICE: Comparativo Detalhado

### Features por Plataforma

| Feature | HubSpot | Pipedrive | DIAX Atual | DIAX + Roadmap |
|---------|---------|-----------|-----------|---|
| **Email Composer** | Drag-drop | Simples | Rich text | Rich text ✓ |
| **Email Scheduling** | Data/hora + optimize | Imediato | Imediato | Imediato |
| **Campaign Reports** | Detalhados | Funil simples | Números | Gráficos ✓ |
| **Contact Timeline** | Completa | Simples | ❌ | Completa ✓ |
| **Lead Scoring** | IA + transparent | Simples | Backend only | Transparent ✓ |
| **Email Sequences** | ✅ Avançado | Simples | ❌ | Básico ✓ |
| **Workflow Automation** | Muito avançado | Médio | ❌ | Básico ✓ |
| **Dashboard** | Muito completo | Simples | ❌ | Completo ✓ |
| **Mobile** | ✅ Otimizado | ✅ Otimizado | ⚠️ Parcial | ✅ Otimizado ✓ |
| **Price** | $50-5000/mo | $10-100/mo | $0 | $0 |

---

## REFERÊNCIAS

- Pipedrive CRM Best Practices (2025)
- HubSpot Email Marketing Guide (2025)
- Salesforce Einstein Lead Scoring Documentation
- ActiveCampaign Automation Playbook
- Industry Reports: G2 CRM Reviews, Forrester Wave

---

**Documento preparado para:** Alexandre Queiroz Marketing Digital
**Data:** Março 2026
**Próxima Revisão:** Após implementação da Fase 1
