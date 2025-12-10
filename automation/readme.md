\# Automation – DIAX CRM (Digital Controller)

A pasta \*\*automation\*\* concentra todos os fluxos de automação do ecossistema DIAX CRM, incluindo:



\- Workflows do \*\*n8n\*\*

\- Scripts auxiliares (Python, Node.js, Bash)

\- Integrações com serviços internos e externos

\- Webhooks

\- Rotinas programadas

\- Documentação de processos automatizados



Esta camada é responsável por \*\*orquestrar\*\* as operações entre:



\- CRM Web (Next.js)

\- API Core (.NET)

\- Scraper Google Email (Python)

\- Banco de dados

\- Sistemas de email

\- Serviços de AI

\- Servidores externos (quando aplicável)



---



\## 🎯 Objetivo



Criar uma camada de automação clara, modular e escalável que execute:



\- Coletas automatizadas de leads

\- Disparo de execuções do scraper

\- Rotinas de limpeza e normalização de dados

\- Atualizações programadas no banco via API Core

\- Geração de relatórios

\- Integração com IA para geração de queries ou classificações

\- Notificações e eventos internos



A automação é a “cola” que conecta todo o ecossistema.



---



\## 🧱 Estrutura recomendada da pasta



```text

automation/

&nbsp;   n8n/

&nbsp;       workflows/               # arquivos .json exportados do n8n

&nbsp;       triggers/                # webhooks e acionadores externos

&nbsp;       templates/               # fluxos reutilizáveis

&nbsp;       docs/                    # explicações dos fluxos

&nbsp;   scripts/

&nbsp;       python/                  # scripts auxiliares em Python

&nbsp;       node/                    # scripts Node.js

&nbsp;       shell/                   # scripts bash

&nbsp;   integrations/

&nbsp;       email/                   # envio de emails (opcional)

&nbsp;       ai/                      # prompts, chamadas GPT, etc.

&nbsp;   README.md



