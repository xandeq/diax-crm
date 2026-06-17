# Plano de Métricas e Monitoramento — Campanha Piloto

Este documento define as metas mínimas de desempenho, limites aceitáveis de entregabilidade e regras duras de interrupção imediata (circuit breakers) para o piloto de 10 agências.

## 1. Metas do Piloto (Baseline Esperado)
Com uma amostragem restrita de 10 leads, cada disparo individual representa 10% da métrica. O objetivo do piloto é técnico e de validação de fluxo, não estatístico, mas serve para calibrar o funil:

- **Bounce Rate (Taxa de Devolução)**: **0%** (Meta ideal) / Máximo tolerado de **0%** (já que a lista de 10 leads passou por higienização ativa).
- **Open Rate (Taxa de Abertura)**: **> 40%** (mínimo de 4 aberturas).
- **Click Rate (Taxa de Cliques no CTA)**: **> 10%** (mínimo de 1 clique).
- **Reply Rate (Taxa de Resposta Direct)**: **> 10%** (mínimo de 1 resposta, mesmo que negativa).
- **Unsubscribe/Spam Complaints**: **0%** (nenhum report ou descadastro).

## 2. Regras de Interrupção Imediata (Circuit Breakers)
O envio da sequência de e-mails subsequentes (Dia 4, Dia 8) ou a expansão da lista de contatos deve ser **imediatamente pausada** se qualquer um dos seguintes gatilhos for atingido:

1. **Ocorrência de Hard Bounce (Qualquer número)**:
   - Se 1 e-mail falhar na entrega definitiva (Hard Bounce), pausar o piloto. Indica falha na higienização inicial (ZeroBounce/NeverBounce) ou bloqueio estrito do domínio de disparo.
2. **Reclamação de Spam (Spam Report - Qualquer número)**:
   - Se qualquer um dos destinatários marcar o e-mail como spam, pausar imediatamente a campanha. Indica que o tom da mensagem está muito invasivo ou que o consentimento (`ConsentStatus`) não é legítimo.
3. **Links Quebrados ou Instabilidade na Landing**:
   - Monitoramento diário da URL `/landing/agencias-digitais`. Qualquer erro 404/500 ou quebra de fluxo de conversão exige pausa imediata.
4. **Queda de Reputação ou Blacklist do Domínio**:
   - Monitorar reputação em ferramentas como Google Postmaster Tools e SenderScore. Se o domínio de disparo cair de classificação ou for inserido em listas de bloqueio temporárias (RBLs), pausar todo o tráfego.

## 3. Monitoramento e Dashboard (Brevo Webhooks)
Toda a telemetria será capturada pelos webhooks integrados no `BrevoWebhookController`:
- **Entregas**: Status `delivered` incrementa a contagem de entrega.
- **Leituras**: Status `opened` registra abertura e vincula o horário.
- **Cliques**: Status `clicked` grava o clique nos links com UTMs correspondentes.
- **Bounces**: Status `bounce` ativa a supressão imediata do contato no banco.
- **Descadastro**: Status `unsubscribe` marca o lead com `EmailOptOut = true`.

---

> [!WARNING]
> **Interrupção Manual**: Em caso de anomalia, o operador deve pausar a campanha movendo o status de `Sending` para `Draft` ou executando a interrupção de envios na fila via API ou console de banco de dados.

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
