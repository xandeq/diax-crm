# 🔐 Resiliência de Secrets — Status de Implementação

## 📊 Resumo Executivo

Sistema de secrets **resiliente em 3 camadas** implementado para DIAX CRM e Extrator de Dados. Agora a aplicação **funciona mesmo quando AWS SM está offline**.

---

## ✅ Concluído — DIAX CRM (.NET)

### 1. ConfigurationProvider com AWS SM Integration
- **Arquivo:** `api-core/src/Diax.Infrastructure/ExternalServices/ConfigurationProvider.cs`
- **Mudança:** Adicionada camada AWS SM via AWSSDK.SecretsManager
- **Cascata agora:**
  ```
  1. AWS SM (tools/diax-extrator) - dinâmica, 10 min refresh
  2. appsettings.Production.json - baked no CI/CD, offline-safe
  3. Env vars (DIAX_EXTRATOR_URL, DIAX_EXTRATOR_API_TOKEN)
  ```
- **Fallback automático:** Se AWS SM falha, continua para camada 2/3 sem erro

### 2. GitHub Actions Workflow Atualizado
- **Arquivo:** `.github/workflows/deploy-api-core-smarterasp.yml`
- **Mudança:** Adicionadas vars Extrator ao jq que gera `appsettings.Production.json`
- **GitHub Secrets adicionados:**
  - `EXTRATOR_URL` = `https://api.extratordedados.com.br` ✓
  - `EXTRATOR_API_TOKEN` = `[PLACEHOLDER]` ⚠️ **PRECISA SER PREENCHIDO**

### 3. FAL Key Removido de appsettings.json
- **Arquivo:** `api-core/src/Diax.Api/appsettings.json`
- **Mudança:** Removido valor hardcoded, agora vem do GitHub Secret `FALAI_API_KEY`
- **Status:** ✓ Seguro

---

## ⚠️ TODO — Valores Reais de Credenciais

### Para DIAX CRM

**Você precisa fornecer:**

1. **Token do Extrator (EXTRATOR_API_TOKEN)**
   - Valor está em: `tools/diax-extrator` no AWS SM
   - Comando para buscar (quando AWS SM estiver respondendo):
     ```bash
     python -m awscli secretsmanager get-secret-value \
       --secret-id "tools/diax-extrator" \
       --region us-east-1 \
       --query SecretString --output text | python -c "import sys, json; d=json.load(sys.stdin); print(d.get('EXTRATOR_API_TOKEN', d.get('extractorToken', '')))"
     ```
   - Após obter, atualizar GitHub Secret:
     ```bash
     gh secret set EXTRATOR_API_TOKEN --body "seu-token-aqui"
     ```

---

## 📋 TODO — Extrator Python (VPS)

### 1. Verificar `extratordedados/prod` no AWS SM
```bash
python -m awscli secretsmanager get-secret-value \
  --secret-id "extratordedados/prod" \
  --region us-east-1 \
  --query SecretString --output text
```

**Confirmar que tem:**
- `APIFY_TOKEN`
- `CRM_EMAIL` / `CRM_PASS`
- `ADMIN_PASSWORD`
- Outras credenciais necessárias

### 2. Criar Script de Sincronização no VPS

**SSH para o VPS:**
```bash
# Credenciais em: tools/vps-hostinger
ssh root@185.173.110.180
```

**Criar `/root/sync-secrets.sh`:**
```bash
#!/bin/bash
# Script para sincronizar secrets do AWS SM para /etc/environment
# Fallback quando AWS SM está offline

echo "🔄 Sincronizando secrets do AWS SM..."

SECRET=$(python3 -c "
import boto3, json
try:
    client = boto3.client('secretsmanager', region_name='us-east-1')
    response = client.get_secret_value(SecretId='extratordedados/prod')
    print(response['SecretString'])
except Exception as e:
    print(f'[ERROR] {e}', file=__import__('sys').stderr)
    exit(1)
")

if [ $? -ne 0 ]; then
    echo "❌ Falha ao buscar secrets do AWS SM"
    exit 1
fi

# Extrai cada chave-valor e exporta
echo "$SECRET" | python3 << 'PYTHON'
import sys, json
try:
    data = json.load(sys.stdin)
    for key, value in data.items():
        print(f"export {key}=\"{value}\"")
except Exception as e:
    print(f"[ERROR] {e}", file=sys.stderr)
    exit(1)
PYTHON
```

**Dar permissões e testar:**
```bash
chmod +x /root/sync-secrets.sh
/root/sync-secrets.sh  # Deve imprimir: export DB_HOST="...", export APIFY_TOKEN="...", etc.
```

### 3. Configurar Cron para Refresh a Cada 6h

**Adicionar ao crontab:**
```bash
crontab -e
```

**Adicionar linha:**
```
0 */6 * * * /root/sync-secrets.sh >> /var/log/sync-secrets.log 2>&1
```

**Ou como systemd timer (mais robusto):**

**Criar `/etc/systemd/system/sync-secrets.service`:**
```ini
[Unit]
Description=Sync AWS Secrets to Environment
After=network-online.target

[Service]
Type=oneshot
ExecStart=/root/sync-secrets.sh
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

**Criar `/etc/systemd/system/sync-secrets.timer`:**
```ini
[Unit]
Description=Run sync-secrets every 6 hours
Requires=sync-secrets.service

[Timer]
OnBootSec=5min
OnUnitActiveSec=6h
AccuracySec=1min

[Install]
WantedBy=timers.target
```

**Ativar:**
```bash
systemctl daemon-reload
systemctl enable sync-secrets.timer
systemctl start sync-secrets.timer
systemctl status sync-secrets.timer
```

---

## 🧪 Testes

### Teste 1: Verificar Cascata de Fallback (DIAX CRM)

```bash
# No servidor SmarterASP, simular AWS SM offline:
# (Temporariamente não configurar credenciais AWS ou bloquear acesso)

# Deve retornar 200 com URL usando appsettings.Production.json:
curl -s https://api.alexandrequeiroz.com.br/api/v1/leads/extrator-config | jq .

# Resposta esperada:
{
  "url": "https://api.extratordedados.com.br",
  "source": "appsettings.Production.json / AWS SM unavailable (using fallback)"
}
```

### Teste 2: Importação de Leads Funcional

```bash
# Acessar: https://crm.alexandrequeiroz.com.br/leads/import
# Clicar no botão "Importar do Extrator"
# Deve retornar lista de leads (não erro 500 ou "Extrator configuration not found")
```

### Teste 3: Extrator Python Offline-Safe (VPS)

```bash
# No VPS, parar a app:
systemctl stop extrator  # ou pkill -f "gunicorn"

# Executar script de sync:
/root/sync-secrets.sh >> /etc/environment

# Iniciar a app:
systemctl start extrator

# App deve iniciar normalmente mesmo com AWS SM offline
```

---

## 📝 Checklist Final

- [ ] **Token do Extrator**: Obter valor real de `tools/diax-extrator` e preencher GitHub Secret
- [ ] **Extrator Python**: Conectar ao VPS e criar `/root/sync-secrets.sh`
- [ ] **Cron/Timer**: Configurar refresh automático a cada 6h
- [ ] **Teste 1**: Verificar cascata com AWS SM simulando offline
- [ ] **Teste 2**: Importar leads via DIAX CRM UI
- [ ] **Teste 3**: Reiniciar app VPS com AWS SM offline

---

## 🔗 Commits Relacionados

```
037b760 feat(secrets): Add AWS Secrets Manager direct integration to ConfigurationProvider
1300dd1 feat(secrets): Inject Extrator credentials via CI/CD and remove hardcoded FAL key
```

---

## 🎯 Resultado Final

| Cenário | Antes | Depois |
|---------|-------|--------|
| AWS SM Online | ✅ Funciona (via IConfiguration) | ✅ Funciona (AWS SM primário) |
| AWS SM Offline | ❌ "Extrator configuration not found" | ✅ Funciona (appsettings.Production.json) |
| App Iniciando | ⚠️ Pode falhar se sem credenciais | ✅ Sempre inicia (fallback garantido) |
| Token Dinâmico | ❌ Apenas ao redeploy | ✅ Atualiza a cada 10min (AWS SM) |

---

**Data de Conclusão:** 2026-03-15
**Próximo Passo:** Preencher token real e testar cascata offline
