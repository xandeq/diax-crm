# 🔧 Aplicar Migration da Agenda no Banco de Produção

## ⚠️ Problema Identificado

O erro ao acessar a página da Agenda (`https://crm.alexandrequeiroz.com.br/agenda/`) em produção ocorre porque o **banco de dados de produção ainda não possui a tabela de compromissos (`appointments`)**.

O deploy do GitHub Actions (via FTP para o SmarterASP) atualizou o código, mas **as migrations do Entity Framework precisam ser aplicadas manualmente** no banco de dados.

---

## ✅ Solução: Executar Script SQL

Um script SQL foi gerado especificamente para criar as tabelas da Agenda.

### Passos para aplicar:

1. **Acesse o painel de administração do seu banco de dados** (site4now.net ou SQL Server Management Studio, da mesma forma que faz normalmente).
   - Server: `sql1002.site4now.net` (ou o atual)
   - Database: `db_aaf0a8_diaxcrm`
   - User: `db_aaf0a8_diaxcrm_admin`

2. **Execute o seguinte script SQL** (copie o código abaixo e rode no Query Editor):

```sql
BEGIN TRANSACTION;
GO

CREATE TABLE [appointments] (
    [id] uniqueidentifier NOT NULL,
    [title] nvarchar(256) NOT NULL,
    [description] nvarchar(256) NULL,
    [date] datetime2 NOT NULL,
    [type] int NOT NULL,
    [daily_notification_sent] bit NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [created_at] datetime2 NOT NULL,
    [created_by] nvarchar(256) NULL,
    [updated_at] datetime2 NULL,
    [updated_by] nvarchar(256) NULL,
    CONSTRAINT [PK_appointments] PRIMARY KEY ([id])
);
GO

CREATE INDEX [IX_appointments_user_id] ON [appointments] ([user_id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303003214_AddAgendaAppointments', N'8.0.11'); -- Atenção: use este valor exato
GO

COMMIT;
GO
```

---

## 🔍 Verificação Pós-Aplicação

3. **Teste novamente:**
   Após dar o comando executado no painel do seu banco e ver a mensagem de sucesso (Comandos concluídos com êxito), recarregue a página:
   [https://crm.alexandrequeiroz.com.br/agenda/](https://crm.alexandrequeiroz.com.br/agenda/)

O erro deve desaparecer e o calendário aparecerá em branco (pronto para novos agendamentos)!
