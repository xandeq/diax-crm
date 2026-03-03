# Configuração da Notificação Diária da Agenda

Para que os e-mails de resumo da agenda do dia sejam enviados às 06:00 da manhã, é necessário configurar um CRON Job externo que acesse o endpoint de disparo.

### Endpoint:
`POST https://<seu-dominio>/api/v1/appointments/trigger-daily-notification`

### Autenticação (Header):
O endpoint não exige token Bearer do usuário, pois é disparado por um sistema. Em vez disso, envia-se um Header de segurança:
`X-Cron-Key: diax-cron-dev-key`
*(A chave por padrão é 'diax-cron-dev-key', mas pode ser alterada via variável de ambiente 'CRON_SECURITY_KEY' no servidor)*

### Exemplo de ferramenta gratuita para CRON:
Você pode utilizar o serviço **cron-job.org** ou **GitHub Actions**.

**Exemplo de configuração no cron-job.org:**
1. Crie uma conta no cron-job.org.
2. Adicione um novo "Cronjob".
3. URL: `https://<seu-dominio>/api/v1/appointments/trigger-daily-notification`
4. Method: `POST`
5. Headers:
   - `X-Cron-Key`: `diax-cron-dev-key`
6. Schedule: `0 6 * * *` (Todos os dias às 06:00:00 da manhã)
   Fuso Horário: `America/Sao_Paulo` (Horário de Brasília)

Quando executado, o sistema buscará os compromissos do dia para cada usuário logado e adicionará na fila de E-mails do CRM para serem despachados via Brevo ou SMTP.
