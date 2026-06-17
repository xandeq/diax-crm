# Checklist Pré-Piloto — Outbound Cold Email B2B

Este documento descreve os pré-requisitos e verificações obrigatórias que devem ser executadas e validadas antes de realizar qualquer envio da campanha piloto para as 10 agências selecionadas.

## 1. Configuração do Domínio de Disparo (mail.diaxcrm.com.br)
- [ ] **SPF (Sender Policy Framework)**: Registro TXT ativo no DNS autorizando o Brevo/provedor a enviar e-mails pelo domínio.
- [ ] **DKIM (DomainKeys Identified Mail)**: Chave pública registrada no DNS para assinatura criptográfica de cabeçalho.
- [ ] **DMARC (Domain-based Message Authentication)**: Política DMARC ativa (pelo menos `p=none`) no subdomínio.
- [ ] **Custom Return-Path**: CNAME apontando para o servidor MX do provedor, alinhando o Return-Path ao domínio visível do remetente.
- [ ] **Caixa Postal Ativa para Reply-To**: Configurar endereço de resposta monitorado e válido (evitar `no-reply@`).

## 2. Validação Higiênica da Lista Piloto
- [ ] **Limite de 10 Leads**: O arquivo CSV/JSON contém exatamente ou no máximo 10 contatos.
- [ ] **Higienização Externa**: Lista passou por validação ativa (ZeroBounce, NeverBounce, Bouncer ou similar).
- [ ] **Exclusão de Status Críticos**: Nenhum contato tem status de validação classificado como `invalid`, `disposable`, `bounce` ou `catch-all`.
- [ ] **Status de Consentimento**: Todos os leads possuem `ConsentStatus` válido registrado (ex: `consentido`, `legitimo_interesse`).
- [ ] **Filtro de Opt-out e Supressão**: Nenhum e-mail pertence à lista de supressão ativa ou possui tag de opt-out no sistema.

## 3. Conformidade de Conteúdo e Links
- [ ] **Link de Descadastro (Unsubscribe)**: Variável `{{unsubscribe_url}}` presente de forma legível no rodapé do HTML e texto puro.
- [ ] **Tracking UTM**: Todos os links (`href`) direcionados para a landing page contêm as variáveis obrigatórias `utm_source`, `utm_medium`, `utm_campaign`, `utm_content` e `utm_term` de acordo com a variante.
- [ ] **Alternativa em Texto Puro (Plain Text)**: O template possui formato alternativo sem tags HTML para evitar filtros de spam.
- [ ] **Assunto e Conteúdo**: Conteúdo revisado para evitar spamwords (ex: "grátis", "compre agora", termos em letras maiúsculas excessivas).

## 4. Testes Internos e Validação Visual
- [ ] **Dry-run de Importação**: Executado o dry-run na API sem registrar erros no lote de 10 agências.
- [ ] **Send-Test de Email**: Enviado teste interno de cada uma das 3 etapas da campanha para o e-mail do próprio administrador logado.
- [ ] **Validação Visual (Gmail/Outlook)**: E-mail renderiza perfeitamente no cliente de e-mail (Desktop).
- [ ] **Validação Mobile**: Layout responsivo verificado no celular.
- [ ] **Verificação de Tokens**: Confirmar que tokens como `{{nome}}`, `{{empresa}}`, `{{ferramenta_atual}}` e `{{dor_principal}}` renderizam os dados reais do lead corretamente e não exibem chaves vazias.

---

> [!CAUTION]
> **Bloqueio de Ativação**: A campanha deve permanecer no estado `Draft` até que todos os itens acima estejam checados com `[x]`. Qualquer desvio anula a homologação do piloto e pode afetar a reputação do domínio.

## Hardening Operacional do Piloto

### 1. Gates de Segurança contra Envio Acidental
- **Bloqueio de Rascunho (Draft Gate)**: A campanha de cold email está restrita ao estado `Draft` no banco de dados. Qualquer tentativa de enfileirar e-mails reais no worker ou via API para uma campanha em rascunho é barrada na validação de prontidão (`ValidateReadiness`) e logada como `PilotCampaignActivationBlocked`.
- **Limite de 10 Leads**: O serviço de importação outbound bloqueia e aborta qualquer tentativa de importar mais de 10 leads se a flag `DryRun` for falsa, registrando o evento `PilotImportBlocked`.
- **Restrição de Ambiente**: O `ASPNETCORE_ENVIRONMENT` deve estar de acordo com os ambientes permitidos (`Development`, `Production` ou `Test`) para permitir qualquer processamento. Ambientes desconhecidos ou não permitidos bloqueiam o envio imediatamente.

### 2. Funcionamento do Circuit Breaker do Piloto
- O circuit breaker operacional (`IPilotCircuitBreaker`) monitora continuamente a saúde da campanha e entra em estado **ABERTO** (bloqueando novos envios) nos seguintes casos:
  - **Bounce Crítico (Hard Bounce)**: Qualquer hard bounce detectado via webhook do Brevo abre o circuito imediatamente.
  - **Falha de Autenticação (Brevo 401)**: Falhas de autenticação com a API Key do Brevo no worker abrem o circuito imediatamente.
  - **Erros Consecutivos de Webhook**: Se ocorrerem 3 ou mais falhas consecutivas ao tentar ler webhooks do Brevo, o circuito abre.
  - **Taxa de Erro Elevada**: Se a taxa de falha nos últimos 10 envios for maior ou igual a 30% (com pelo menos 3 disparos), o circuito abre.
- Quando o circuito abre, novas tentativas de disparo/enfileiramento são impedidas no backend e logadas como `PilotRealSendBlocked` e `PilotCircuitBreakerOpened`.

### 3. Validação de Dry Run
- A importação suporta `DryRun: true`. Ao executar a importação com esta flag, o sistema valida todos os leads (duplicatas, e-mails, tags) e simula o resultado sem persistir dados no banco de dados. Os eventos de auditoria registrados são `PilotDryRunStarted` e `PilotDryRunCompleted` (ou `PilotDryRunFailed`).

### 4. Validação de Send Test (Isolamento Completo)
- O `SendTestEmailAsync` está restrito ao administrador autenticado no painel. O e-mail de teste só pode ser enviado para o e-mail do próprio administrador logado.
- Mocks seguros de variáveis do piloto (como `{{nome}}`, `{{empresa}}`, `{{ferramenta_atual}}`, etc.) são usados para substituir dados de leads reais, garantindo isolamento total.
- O teste de envio **não** cria leads reais, não atualiza métricas e não contamina estatísticas da campanha real.

### 5. Investigação de Bloqueios e Confirmação de Envio Real Zero
- No painel operacional do frontend (`/campaigns/agencias-digitais`), o operador pode monitorar a taxa de erros, o status do circuit breaker, o checklist de prontidão e a trilha de auditoria de eventos em tempo real.
- Em caso de bloqueio, os detalhes do erro/bloqueio são registrados na tabela de auditoria (`recentEvents` obtidos do banco), listando a ação (ex: `PilotImportBlocked`) e os motivos em `blockingReasons`.
- Para confirmar que nenhum envio real ou enfileiramento ocorreu, o operador pode verificar se a contagem de e-mails enviados/enfileirados permanece em zero e se não há registros de e-mail na fila (`EmailQueueItem`) com `CampaignId` da campanha real além de logs de teste.

### 6. Condições para Liberar os 10 Envios do Piloto
Antes de mover a campanha para o estado de disparo e liberar os 10 envios controlados, as seguintes condições devem ser atendidas e checadas:
1. Todos os registros DNS (SPF, DKIM, DMARC) devem ser ativados e validados no subdomínio secundário `mail.diaxcrm.com.br`.
2. Um e-mail de teste interno deve ser enviado e renderizado com sucesso sem quebras visuais.
3. O template deve possuir o link de cancelamento de inscrição (`{{unsubscribe_url}}`) e todas as URLs rastreadas com UTMs corretas.
4. O circuit breaker deve estar no estado **FECHADO**.
5. O operador deve alterar manualmente o status da campanha no banco ou via painel quando a prontidão for 100% verde.
