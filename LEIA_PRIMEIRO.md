# 🚨 CORREÇÃO URGENTE - Sistema de Importação

## ❌ Problema Identificado

A funcionalidade de **importação de leads/clientes** está gerando erro em produção:

```
SqlException: Invalid object name 'customer_imports'.
```

**Causa:** A migração do banco de dados foi criada mas ficou **vazia**, então a tabela não foi criada.

---

## ✅ Solução Rápida (5 minutos)

### Opção 1: Automático (RECOMENDADO) 🚀

1. Abra **PowerShell como Administrador** na raiz do projeto
2. Execute:
   ```powershell
   .\fix-customer-imports-migration.ps1
   ```
3. Aguarde a conclusão (você verá ✓ em cada etapa)
4. Pronto! Teste a importação.

---

### Opção 2: Manual (se preferir controle total) 🔧

Siga o passo a passo detalhado em: **`COMANDOS_MANUAIS.md`**

---

## 🎯 O que será feito

1. ✋ Parar API local (se estiver rodando)
2. 🗑️ Remover migração vazia
3. 🔨 Reconstruir projeto
4. ✨ Criar migração correta
5. 💾 Aplicar no banco LOCAL
6. 📄 Gerar script SQL para PRODUÇÃO

---

## ⚠️ Importante

- ✅ **Banco LOCAL:** Será atualizado automaticamente
- ⚠️ **Banco PRODUÇÃO:** Você precisa aplicar o script SQL manualmente

**Arquivo gerado para produção:**
```
apply_customer_imports_production.sql
```

**Como aplicar em produção:**
1. Fazer **BACKUP** do banco de produção primeiro!
2. Abrir SSMS (SQL Server Management Studio)
3. Conectar ao servidor de produção
4. Executar o script `apply_customer_imports_production.sql`

---

## 📚 Arquivos de Ajuda

| Arquivo | Descrição |
|---------|-----------|
| `fix-customer-imports-migration.ps1` | Script PowerShell automático |
| `COMANDOS_MANUAIS.md` | Comandos passo a passo |
| `RESOLVER_IMPORTACAO.md` | Documentação completa do problema |

---

## 🧪 Como Testar Depois

### 1. Iniciar API
```bash
cd api-core/src/Diax.Api
dotnet run
```

### 2. Acessar frontend
```
http://localhost:3000/leads/import
```

### 3. Testar importação com CSV

**Criar arquivo `teste.csv`:**
```csv
Nome,Email,Telefone
João Silva,joao@example.com,11987654321
Maria Santos,maria@example.com,11987654322
Pedro Costa,pedro@example.com,11987654323
```

**OU colar texto direto:**
```
João Silva
joao@example.com
(11) 98765-4321

Maria Santos
maria@example.com
11 98765-4322
```

### 4. Verificar resultado

**No SQL Server:**
```sql
-- Ver registros importados
SELECT * FROM customer_imports
ORDER BY CreatedAt DESC;

-- Ver contatos criados
SELECT * FROM customers
WHERE CreatedAt >= CAST(GETDATE() AS DATE)
ORDER BY CreatedAt DESC;
```

---

## 🆘 Precisa de Ajuda?

**Se encontrar erro:**
1. Copie a mensagem de erro completa
2. Verifique o arquivo de log em `api-core/src/Diax.Api/logs/`
3. Consulte a seção "Troubleshooting" em `COMANDOS_MANUAIS.md`

**Erros comuns:**

| Erro | Solução |
|------|---------|
| "Cannot access file" | Feche Visual Studio / Rider, pare todos os processos dotnet |
| "Build failed" | Verifique erros de compilação, corrija-os primeiro |
| "Cannot open database" | Verifique se SQL Server está rodando |
| "Permission denied" | Execute PowerShell como Administrador |

---

## 📊 Status Atual

- ✅ Backend (API) implementado
- ✅ Frontend (React) implementado
- ✅ Entidade `CustomerImport` criada
- ✅ Service `CustomerImportService` criado
- ✅ Configuração EF `CustomerImportConfiguration` criada
- ❌ **Migração do banco (VAZIA - precisa ser corrigida)**
- ❌ Tabela `customer_imports` (não existe)

Depois desta correção, tudo estará ✅!

---

## 🎉 Depois de Corrigir

A funcionalidade completa de importação estará disponível:

- 📁 Upload de CSV (detecta `;` ou `,`)
- 📝 Cola de texto (extrai nome, email, telefone)
- ✅ Validação de email
- 🔄 Botão "Converter Lead → Cliente"
- 💬 Botão WhatsApp (abre WhatsApp Web)
- 📧 Botão Email (abre mailto)
- 📊 Histórico de importações

---

**Data:** 15/02/2026 21:10
**Prioridade:** 🔴 ALTA
**Tempo estimado:** ⏱️ 5-10 minutos
