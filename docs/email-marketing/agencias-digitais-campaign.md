# Campanha: Agências Digitais BR — DIAX CRM

Este documento detalha as especificações da campanha de prospecção fria para agências digitais brasileiras de médio porte (5 a 30 funcionários) no DIAX CRM.

---

## 1. Visão Geral da Campanha
* **Público-Alvo**: Sócios, Diretores e decisores de agências de marketing e publicidade digital no Brasil (tamanho de 5 a 30 colaboradores).
* **Problema a Resolver**: A fragmentação operacional entre ferramentas (leads no CRM tradicional, conversas de vendas no WhatsApp pessoal e controle financeiro em planilhas ou Notion).
* **Solução Proposta**: DIAX CRM — CRM centralizado, com WhatsApp nativo, faturamento automático pós-venda e leads enriquecidos por IA, por um preço fixo de R$ 300/mês.
* **Objetivo da Campanha**: Agendamento de uma demonstração de 20 minutos de forma guiada através da landing page `/landing/agencias-digitais`.

---

## 2. Estrutura da Sequência e Variantes A/B

A campanha possui 3 etapas de contato com intervalos definidos, operando com divisão 50/50 de variantes de teste.

```
                    [ Entrada no Segmento de Leads ]
                                   │
                                 Dia 1
                       ┌───────────┴───────────┐
                   Variante A              Variante B
             (Foco: WhatsApp Nativo)   (Foco: Consolidação/Custo)
                                   │
                                 Dia 4
                       ┌───────────┴───────────┐
                   Variante A              Variante B
             (Foco: Controle Operacional) (Foco: Retrabalho Financeiro)
                                   │
                                 Dia 8
                       ┌───────────┴───────────┐
                   Variante A              Variante B
               (Foco: IA de Leads)       (Foco: Breakup Direto)
```

### E-mail 1 (Dia 1)
* **Objetivo**: Apresentar a dor da fragmentação de forma educada e propor a solução integrada.
* **Variantes de Assunto**:
  * **A**: `whatsapp na {{empresa}}`
  * **B**: `consolidação de ferramentas na {{empresa}}`
* **Preheaders**:
  * **A**: *O histórico de conversas comerciais da sua agência fica salvo no CRM ou no celular do vendedor?*
  * **B**: *Como simplificar a pilha de vendas e faturamento da sua agência hoje.*

### E-mail 2 (Dia 4)
* **Objetivo**: Aprofundar em dores específicas de controle comercial e velocidade de cobrança.
* **Variantes de Assunto**:
  * **A**: `whatsapp comercial da {{empresa}}`
  * **B**: `faturamento de novos clientes na {{empresa}}`
* **Preheaders**:
  * **A**: *O histórico de negociação da sua equipe de vendas fica centralizado ou vai embora com o vendedor?*
  * **B**: *Como reduzir o tempo entre a assinatura do contrato e a emissão do primeiro boleto.*

### E-mail 3 (Dia 8)
* **Objetivo**: Última tentativa de contato com foco em qualificação inteligente ou encerramento direto de fluxo.
* **Variantes de Assunto**:
  * **A**: `enriquecimento de leads com IA na {{empresa}}`
  * **B**: `última tentativa: {{empresa}}`
* **Preheaders**:
  * **A**: *Como qualificar e encontrar dados dos contatos comerciais automaticamente.*
  * **B**: *Centralize seu comercial, WhatsApp e financeiro em português por R$ 300/mês.*

---

## 3. Mapeamento de Tokens e Variáveis
O DIAX CRM resolve e compila as seguintes variáveis no momento do disparo de forma dinâmica (conforme testes executados em [EmailCampaignStrategyTests.cs](file:///d:/claude-code/diax-crm/api-core/tests/Diax.Tests/Application/EmailMarketing/EmailCampaignStrategyTests.cs)):

* `{{nome}}`: Primeiro nome do contato resolvido pelo módulo de normalização de nomes.
* `{{empresa}}`: Razão social ou nome fantasia da agência cadastrada.
* `{{site}}`: Website registrado do lead.
* `{{cidade}}`: (Retornará vazio — campo de dados não mapeado na modelagem de banco de dados do cliente atual).
* `{{ferramenta_atual}}`: Resolvido automaticamente a partir da análise de tags do lead (detecta tags como `pipedrive`, `rd station`, `notion` ou `planilha`).
* `{{dor_principal}}`: Resolvido a partir de tags do lead, segmentando entre problemas de WhatsApp ou problemas de cobrança manual.
* `{{cta_link}}`: Direcionado para `https://diaxcrm.com.br/landing/agencias-digitais` acoplado com as tags de tracking correspondentes.
* `{{unsubscribe_url}}`: Link único e criptografado para opt-out de marketing.

---

## 4. Estrutura de Tracking e Destinos

Os links de chamada para ação de todas as variantes devem seguir o mapeamento de parâmetros abaixo para alimentar os relatórios de Analytics do DIAX CRM:

* **Landing Page**: `https://diaxcrm.com.br/landing/agencias-digitais`
* **Parâmetros Base**: `?utm_source=cold_email&utm_medium=email&utm_campaign=agencias_digitais_diax_crm`
* **UTM Content por Peça**:
  * Dia 1 / Variante A: `&utm_content=day_1_a`
  * Dia 1 / Variante B: `&utm_content=day_1_b`
  * Dia 4 / Variante A: `&utm_content=day_4_a`
  * Dia 4 / Variante B: `&utm_content=day_4_b`
  * Dia 8 / Variante A: `&utm_content=day_8_a`
  * Dia 8 / Variante B: `&utm_content=day_8_b`
