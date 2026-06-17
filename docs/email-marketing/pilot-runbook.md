# Runbook de Operação Piloto (10 Agências) — DIAX CRM

Este manual instrui como rodar o primeiro lote piloto da campanha **Cold Email Agências Digitais BR** de forma segura, monitorando as entregas e sem risco de quebrar regras de reputação ou LGPD.

---

## Passo 1: Preparação do Domínio
1. Adquira o domínio secundário (ex: `go.diaxcrm.com.br`).
2. Adicione as entradas de DNS configuradas de acordo com o [Checklist de Deliverability](file:///d:/claude-code/diax-crm/docs/email-marketing/deliverability-checklist.md).
3. Aguarde de 24h a 48h para a propagação global dos registros.
4. Execute uma consulta em ferramentas de diagnóstico DNS (como `mxtoolbox.com`) buscando o seu domínio secundário para validar SPF, DKIM e DMARC.

---

## Passo 2: Higienização e Importação de Leads (10 Agências)
*Nunca suba listas sem higienização prévia.*
1. Mapeie 10 agências digitais brasileiras de médio porte (5 a 30 funcionários) que possuam emails corporativos válidos de decisores.
2. Submeta a lista de e-mails em um validador como **NeverBounce**, **ZeroBounce** ou **Bouncer**.
3. **Rejeite imediatamente**:
   * E-mails classificados como *Invalid*, *Catch-all*, *Disposable* ou *Unknown*.
   * Endereços de e-mail genéricos (ex: `contato@`, `vendas@`, `financeiro@`). Utilize apenas contatos individuais nominativos (ex: `joao@`).
4. Prepare o arquivo JSON ou CSV de importação contendo a seguinte estrutura e campos estendidos:

```json
[
  {
    "Name": "Roberto Oliveira",
    "Email": "roberto@agencianova.com.br",
    "CompanyName": "Agência Nova",
    "Website": "www.agencianova.com.br",
    "City": "Rio de Janeiro",
    "CurrentTool": "Pipedrive",
    "MainPain": "Falta de integração com o WhatsApp",
    "ValidationStatus": "valido",
    "ConsentStatus": "consentido",
    "Tags": "outbound_2026"
  }
]
```

5. Submeta a carga através do endpoint `POST /api/v1/customers/import` (ou interface de importação no painel).
6. **Policiamento de Segurança e Mapeamento Automático**:
   * O sistema rejeitará e-mails com formato inválido, contatos com opt-out/unsubscribed, contatos na lista de supressão (com bounce/spam) ou lotes importados sem origem (`Source`).
   * O campo `ValidationStatus` é obrigatório e deve ser positivo (ex: `valido`, `verificado`), impedindo a entrada de e-mails inválidos/desconhecidos/bounced.
   * Todos os leads importados recebem automaticamente a tag `pilot_candidate` para fins de isolamento de público e prevenção de enfileiramento indesejado.
   * O website é mapeado nativamente. As propriedades `CurrentTool` (ex: Pipedrive, Notion, RD Station, Planilha) e `MainPain` (ex: WhatsApp, Financeiro) são mapeadas para tags correspondentes a fim de alimentar o motor de templates da campanha. As demais informações (`City`, `ConsentStatus`) são armazenadas estruturadamente nas notas do lead.

---

## Passo 3: Envio de Teste Interno (Send-Test)
Garante a perfeita renderização e links corretos antes do disparo piloto.

1. Acesse o painel do DIAX CRM em **Marketing / Email Marketing**.
2. Selecione a campanha criada em estado **Draft** (Rascunho).
3. Clique em **Enviar E-mail de Teste** (ou utilize o endpoint `POST /api/v1/email-campaigns/campaigns/{campaignId}/send-test`).
4. Insira o seu próprio email como destino autorizado.
5. Abra a mensagem recebida em pelo menos dois clientes diferentes (ex: **Gmail** no smartphone e **Outlook** no desktop).
6. **Verifique os seguintes itens**:
   * O assunto aparece limpo, curto e em letras minúsculas (se variante correspondente)?
   * O preheader está visível e condizente com a variante de assunto?
   * As variáveis como `{{nome}}` e `{{empresa}}` foram substituídas corretamente pelo mock de teste?
   * Clique nos links da landing page e confirme se os parâmetros de tracking UTM e UTM Content estão anexados corretamente na URL final.
   * Clique no link de `unsubscribe_url` e verifique se ele leva à página de confirmação de descadastro.

---

## Passo 4: Readiness Gate (Portão de Prontidão)
Antes de mudar a campanha de status, marque os itens como validados:

* [ ] E-mail de teste recebido, renderizado e aprovado visualmente.
* [ ] Links de CTA validados com os devidos UTMs de acompanhamento.
* [ ] Variável de descadastro presente no rodapé do HTML e funcionando.
* [ ] Registros SPF, DKIM e DMARC ativos para o subdomínio comercial.
* [ ] Lista de 10 leads higienizada, sem e-mails genéricos ou bounces conhecidos.

---

## Passo 5: Ativação da Campanha Piloto
Com todos os portões de prontidão verificados:

1. Acesse a campanha e agende-a para disparo controlado (alterando o status para **Scheduled** no DIAX CRM).
2. O background worker `EmailQueueProcessorWorker` processará a fila de forma automática nos próximos ciclos de 5 minutos, enviando as mensagens seguindo as restrições de ramping.
3. Acompanhe a entrega em tempo real através do painel de **Relatórios/Analytics** da campanha, monitorando as taxas de abertura e respostas.
4. **Quando Pausar**: Caso ocorra **1 bounce permanente** ou **1 denúncia de spam** dentro do lote piloto de 10 leads, pause imediatamente a campanha para revisar o processo de coleta e validação da lista.
