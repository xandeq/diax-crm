# Database Credentials Reference

## Production Database (SmarterASP.NET)

| Campo | Valor |
|-------|-------|
| Server | sql1002.site4now.net |
| Port | 1433 |
| Database | db_aaf0a8_diaxcrm |
| User (Regular) | db_aaf0a8_diaxcrm |
| Password (Regular) | 10Alexandre10# |
| User (Admin) | db_aaf0a8_diaxcrm_admin |
| Password (Admin) | [Ver AWS Secrets Manager] |

## Connection Strings

### SQL Server
```
Server=tcp:sql1002.site4now.net,1433;Database=db_aaf0a8_diaxcrm;User Id=db_aaf0a8_diaxcrm;Password=10Alexandre10#;TrustServerCertificate=True;Encrypt=True;Connection Timeout=60;
```

### Entity Framework Core
```csharp
optionsBuilder.UseSqlServer(
    "Server=tcp:sql1002.site4now.net,1433;Database=db_aaf0a8_diaxcrm;User Id=db_aaf0a8_diaxcrm;Password=10Alexandre10#;TrustServerCertificate=True;Encrypt=True;Connection Timeout=60;"
);
```

## Storage Locations

- ✅ AWS Secrets Manager: `tools/alexandrequeiroz-db`
- ✅ .NET User Secrets: `~/.microsoft/usersecrets/diax-crm-api-secrets/secrets.json`
- ✅ .env.production (local reference)
- ⚠️ GitHub Secrets: `DB_CONNECTION_STRING` (set via workflow)

## Migration Status

- Migration File: `api-core/src/Diax.Infrastructure/Data/Migrations/20260318110021_AddVideoProviderLimits.cs`
- Auto-applied on startup: YES (via `db.Database.Migrate()`)
- Last deployment: 2026-03-18 16:25 UTC ✓ Success

## Quick Access Commands

```bash
# Fetch from AWS Secrets Manager
aws secretsmanager get-secret-value --secret-id tools/alexandrequeiroz-db --region us-east-1

# Test connection
sqlcmd -S sql1002.site4now.net,1433 -d db_aaf0a8_diaxcrm -U db_aaf0a8_diaxcrm -P "10Alexandre10#" -Q "SELECT @@version"

# Apply migration manually (if needed)
sqlcmd -S sql1002.site4now.net,1433 -d db_aaf0a8_diaxcrm -U db_aaf0a8_diaxcrm -P "10Alexandre10#" -i apply-video-provider-migration.sql
```

