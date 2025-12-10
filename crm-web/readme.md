\# DIAX CRM Web

Interface oficial do \*\*DIAX CRM (Digital Controller)\*\*, desenvolvida em \*\*Next.js\*\*, responsável pela visualização, gestão e operação de todos os dados coletados pelo sistema.



Aqui é onde usuários interagem com o ecossistema: analisam leads, disparam campanhas de coleta, acompanham histórico, definem segmentos e têm controle total sobre o pipeline comercial e de marketing.



---



\## 📌 Objetivo do CRM Web



\- Ser a \*\*interface central de operação\*\* do ecossistema DIAX CRM.

\- Exibir e gerenciar:

&nbsp; - leads coletados pelo Scraper

&nbsp; - campanhas de busca

&nbsp; - histórico de contatos e email

&nbsp; - segmentos de mercado

&nbsp; - métricas e análises

\- Permitir ao usuário disparar ações:

&nbsp; - executar scraping

&nbsp; - atualizar status de leads

&nbsp; - iniciar ou pausar campanhas

&nbsp; - filtrar e organizar dados

\- Facilitar a integração entre:

&nbsp; - API Core

&nbsp; - Scraper

&nbsp; - n8n

&nbsp; - Banco de dados

&nbsp; - Infraestrutura de email



O CRM Web é \*\*o cérebro visual\*\* do sistema.



---



\## 🧱 Tecnologias recomendadas



\- \*\*Next.js 14+ (App Router)\*\*  

\- React 18  

\- TypeScript  

\- Tailwind CSS (opcional)  

\- Zustand ou Redux Toolkit (opcional)  

\- Axios ou fetch nativo  

\- Componentes modulares (design system futuro)



---



\## 📂 Estrutura de diretórios (sugerida)



```text

crm-web/

&nbsp;   src/

&nbsp;       app/                    # Rotas Next.js

&nbsp;           leads/              # Tela de leads

&nbsp;           campaigns/          # Tela de campanhas

&nbsp;           segments/           # Tela de segmentos

&nbsp;           history/            # Histórico

&nbsp;           settings/           # Configurações

&nbsp;       components/             # Componentes reutilizáveis

&nbsp;       services/               # Consumo da API Core

&nbsp;       hooks/                  # Lógica de estado

&nbsp;       utils/                  # Helpers

&nbsp;       types/                  # Tipos TypeScript

&nbsp;   public/                     # Assets estáticos

&nbsp;   package.json

&nbsp;   next.config.js

&nbsp;   README.md



