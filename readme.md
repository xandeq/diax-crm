# Digital Controller (DIAX CRM)

Este repositório é o **monorepo principal** do sistema de controle operacional, CRM e automação da empresa **Alexandre Queiroz Marketing Digital**, dentro da linha de produtos **DIAX**.

Ele concentra todos os módulos que formam o “controlador digital” da empresa: coleta de leads, armazenamento de dados, automação, CRM, histórico e infraestrutura de deploy.

---

## Visão geral do projeto

O **Digital Controller** é o sistema nervoso da operação digital da empresa.  
Ele integra:

- Coleta automatizada de leads pela internet
- Armazenamento centralizado em banco de dados
- Automação de processos com n8n
- Interface CRM para visualização e gestão
- Infraestrutura de envio e controle de emails
- Histórico completo de contatos, campanhas e eventos

O objetivo é ter um sistema **próprio**, **independente de SaaS externos**, com dados sob controle total, pronto para crescer e receber novos módulos no futuro.

---

## Estrutura de pastas deste monorepo

A raiz do monorepo segue esta estrutura:

```text
digital-controller/
    scraper-google-email/   # Coletor de leads (Python + Selenium + BS4)
    crm-web/                # Frontend CRM (Next.js)
    api-core/               # Backend/API central (.NET / REST)
    automation/             # Fluxos de automação (n8n, scripts, webhooks)
    infra/                  # Infraestrutura, deploy, Docker, CI/CD
    docs/                   # Documentação funcional e técnica
