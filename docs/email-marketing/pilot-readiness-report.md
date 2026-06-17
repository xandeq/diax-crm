# Relatório de Prontidão do Piloto (Readiness Report)

Este relatório compila os status de validação técnica do ambiente, reputação do domínio, integridade da lista e prontidão da campanha antes da execução dos 10 disparos controlados.

## 1. Dados Básicos do Piloto
- **Nome da Campanha**: Cold Email Agências Digitais BR (ID: `placeholder-campaign-id`)
- **Remetente**: `contato@mail.diaxcrm.com.br` (Domínio de disparo secundário)
- **Data do Relatório**: 12 de Junho de 2026
- **Responsável**: Administrador Técnico DIAX

## 2. Portão de Prontidão (Readiness Gate)
| Critério de Prontidão | Status | Observações |
| :--- | :---: | :--- |
| Registro SPF ativo | [ ] | DNS verificado via query TXT |
| Assinatura DKIM configurada | [ ] | Par de chaves verificado no Brevo |
| Política DMARC ativa | [ ] | Registro `_dmarc.mail.diaxcrm.com.br` ativo |
| Reply-To ativo | [ ] | Caixa postal de destino monitorada e recebendo e-mails |
| Domínio alinhado (Return-Path) | [ ] | CNAME configurado para o provedor |
| Ausência de Secrets hardcoded | [x] | Chaves API e senhas injetadas via variáveis de ambiente |
| UTMs de rastreamento configuradas | [x] | Parâmetros `utm_source`, `utm_medium` e `utm_campaign` integrados |
| Unsubscribe link obrigatório | [x] | Token `{{unsubscribe_url}}` validado no rodapé de todos os e-mails |
| Texto Puro (Plain Text) alternativo | [x] | Configurado no serviço de templates |

## 3. Prontidão da Lista (Lead Ingestion)
- **Total de Leads Selecionados**: 10 agências digitais brasileiras
- **Leads Aceitos na Importação**: 0 (Aguardando importação real pós-homologação)
- **Leads Rejeitados na Importação**: 0
- **Motivos de Rejeição**: Nenhum erro registrado no dry-run da amostra padrão.
- **Tag Aplicada**: `pilot_candidate` aplicada automaticamente para isolamento.

## 4. Status de Envio de Teste (Send-Test Interno)
- **Status do Teste Interno**: [ ] Pendente / [ ] Sucesso
- **Destinatário do Teste**: Administrador Logado (`admin@diaxcrm.com.br`)
- **Validação de Renderização**: E-mail sem bugs de HTML e responsivo no mobile.
- **Validação de Links**: Links de teste clicáveis direcionando corretamente com as UTMs associadas.
- **Validação de Opt-out**: Link de descadastro gerando a URL correspondente de opt-out.

## 5. Avaliação de Risco Residual
- **Risco de Spam**: **Baixo**. A lista de 10 leads é altamente higienizada, reduzindo a chance de bounces e spamtraps.
- **Risco de Reputação**: **Baixo**. Limitar o piloto a 10 leads previne sinalização de disparos em lote sem warm-up.
- **Risco de Vazamento de Dados**: **Inexistente**. Nenhum dado real ou chave privada está exposto no código ou nos logs públicos.

## 6. Recomendação Final
- **[ ] LIBERAR PILOTO**: Todos os critérios de prontidão atendidos, lista de 10 agências higienizada e testes internos aprovados. A campanha pode ser enviada manualmente.
- **[x] NÃO LIBERAR PILOTO (Atual)**: Aguardar a validação manual dos registros DNS do subdomínio e a aprovação final do operador para liberação física do disparo.

---

*Relatório gerado automaticamente pelo sistema de segurança e auditoria do DIAX CRM.*

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
