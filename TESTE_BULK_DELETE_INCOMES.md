# Guia de Teste - Correção Bulk Delete de Receitas

## 🎯 Objetivo
Validar a correção do erro "Income not found" ao excluir receitas em massa.

## 📋 Alterações Implementadas

### Backend (API)
1. ✅ **IncomesController.cs** - Adicionado logging detalhado:
   - Log de quantidade de IDs recebidos
   - Validação de IDs vazios (Guid.Empty)
   - Log do UserId resolvido
   - Log de resultado da operação

2. ✅ **IncomeService.cs** - Melhorias de diagnóstico:
   - Validação de Guid.Empty no início do DeleteRangeAsync
   - Log diferenciado quando receita não existe vs. pertence a outro usuário
   - Console logs para rastreamento detalhado

### Frontend
3. ✅ **finance.ts** - Validação de IDs:
   - Regex para validar formato UUID válido
   - Filtragem de IDs inválidos antes do envio
   - Logs de debug no console do navegador
   - Mensagens de erro específicas

---

## 🧪 Cenários de Teste

### Teste 1: Exclusão Normal (Deve Funcionar)
**Ação:**
1. Acessar tela de Receitas (`/finance/incomes`)
2. Selecionar 1-3 receitas válidas
3. Clicar em "Excluir Selecionados"

**Resultado Esperado:**
- ✅ Console do navegador mostra: `🔍 Enviando N IDs para exclusão: [...]`
- ✅ Backend logs mostram: `[BulkDelete] Iniciado com N IDs`
- ✅ Receitas são excluídas com sucesso
- ✅ Mensagem de sucesso exibida
- ✅ Lista atualizada sem as receitas excluídas

**Logs Esperados (Backend):**
```
[Information] [BulkDelete] Iniciado com 3 IDs
[Debug] [BulkDelete] IDs recebidos: 51955c50-3c0d-4282-ac33-1be06eeccf76, ...
[Debug] [BulkDelete] UserId resolvido: {guid}
[Information] [IncomeService.DeleteRangeAsync] Iniciando exclusão de 3 receitas para usuário {guid}
[Information] [BulkDelete] Sucesso - 3 receitas excluídas
```

---

### Teste 2: ID Inválido no Frontend (Deve Filtrar)
**Ação:**
1. Abrir DevTools do navegador (F12)
2. No console, executar:
```javascript
// Simular chamada com IDs inválidos
financeService.deleteIncomesBulk(['invalid-id', '51955c50-3c0d-4282-ac33-1be06eeccf76'])
```

**Resultado Esperado:**
- ⚠️ Console mostra: `⚠️ ID inválido filtrado: invalid-id`
- ⚠️ Console mostra: `⚠️ 1 IDs inválidos foram filtrados`
- ✅ Apenas ID válido é enviado ao backend
- ✅ Exclusão procede normalmente

---

### Teste 3: Todos IDs Inválidos (Deve Rejeitar)
**Ação:**
```javascript
financeService.deleteIncomesBulk(['abc', '123', 'not-a-guid'])
```

**Resultado Esperado:**
- ❌ Erro lançado: `Nenhum ID válido foi fornecido para exclusão`
- ❌ Nenhuma requisição enviada ao backend

---

### Teste 4: Receita de Outro Usuário (Deve Falhar Graciosamente)
**Ação:**
1. Tentar excluir uma receita que pertence a outro usuário
2. (Pode ser simulado via API direta ou alterando banco temporariamente)

**Resultado Esperado Backend:**
```
[IncomeService.DeleteAsync] ERRO: Receita {id} existe mas pertence ao usuário {outrouserId}, tentativa de exclusão por {userId}
[BulkDelete] Falha - Income.NotFound: Income not found
```

**Resultado Frontend:**
- ❌ Mensagem de erro: "Income not found"
- ⚠️ Operação interrompida (rollback de transação)

---

### Teste 5: Receita Inexistente (Identificar Causa)
**Ação:**
1. Tentar excluir um GUID que não existe no banco:
```javascript
financeService.deleteIncomesBulk(['00000000-0000-0000-0000-000000000001'])
```

**Resultado Esperado Backend:**
```
[IncomeService.DeleteAsync] ERRO: Receita 00000000-0000-0000-0000-000000000001 não existe no banco de dados
```

**Resultado Frontend:**
- ❌ Erro retornado com mensagem apropriada

---

### Teste 6: Guid.Empty Detectado (Nova Validação)
**Ação:**
1. Simular envio de Guid.Empty (pode indicar falha na deserialização):
```bash
curl -X POST https://api.diaxcrm.com/api/v1/incomes/bulk-delete \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"ids":["00000000-0000-0000-0000-000000000000"]}'
```

**Resultado Esperado:**
```json
{
  "error": "General.InvalidIds",
  "message": "1 IDs inválidos (vazios) foram enviados"
}
```

**Log Backend:**
```
[Warning] [BulkDelete] 1 IDs inválidos (Guid.Empty) detectados
[Information] [IncomeService.DeleteRangeAsync] ERRO: 1 IDs vazios (Guid.Empty) detectados na lista
```

---

## 🔍 Como Diagnosticar o Erro Original

### Passo 1: Verificar Logs do Frontend
1. Abrir DevTools (F12)
2. Ir para aba "Console"
3. Tentar excluir a receita problemática (`51955c50-3c0d-4282-ac33-1be06eeccf76`)
4. Procurar por:
   - ✅ `🔍 Enviando 1 IDs para exclusão:`
   - ⚠️ `⚠️ ID inválido filtrado:`
   - ❌ Erros de validação

### Passo 2: Verificar Logs do Backend
**Produção (SmarterASP):**
1. Acessar painel de logs da hospedagem
2. Filtrar por: `[BulkDelete]` ou `[IncomeService]`
3. Procurar por:
   ```
   [BulkDelete] Iniciado com 1 IDs
   [BulkDelete] IDs recebidos: 51955c50-3c0d-4282-ac33-1be06eeccf76
   [BulkDelete] UserId resolvido: {guid}
   ```

**Local:**
```powershell
cd api-core
dotnet run --project src/Diax.Api
# Console mostrará logs em tempo real
```

### Passo 3: Verificar Banco de Dados
```powershell
# Conectar ao banco de produção
$cs = (Get-Content "api-core/src/Diax.Api/appsettings.Production.json" | ConvertFrom-Json).ConnectionStrings.DefaultConnection

# Verificar se receita existe
sqlcmd -S "sql1002.site4now.net" -d "db_aaf0a8_diaxcrm" -U "db_aaf0a8_diaxcrm_admin" -Q @"
SELECT
    Id,
    Description,
    Amount,
    UserId,
    FinancialAccountId,
    CreatedAt
FROM Incomes
WHERE Id = '51955c50-3c0d-4282-ac33-1be06eeccf76'
"@
```

**Verificações:**
- ✅ Receita existe? (deve retornar 1 linha)
- ✅ UserId corresponde ao usuário logado?
- ✅ FinancialAccountId é válido?

---

## 📊 Análise de Causas Possíveis

Com os novos logs, será possível identificar:

| Sintoma nos Logs | Causa Raiz | Solução |
|------------------|------------|---------|
| `ID inválido filtrado` no console | Frontend gerando IDs malformados | Validação já implementada ✅ |
| `IDs vazios (Guid.Empty) detectados` | Deserialização JSON falhou | Validação já implementada ✅ |
| `existe mas pertence ao usuário X` | Problema de autenticação/multitenancy | Verificar resolução de UserId |
| `não existe no banco de dados` | Receita já foi excluída ou nunca existiu | Atualizar lista no frontend |
| Request nunca chega ao controller | Problema de roteamento/CORS | Verificar network tab |

---

## ✅ Checklist de Validação

Após deploy das mudanças:

- [ ] **Teste 1** passou - Exclusão normal funciona
- [ ] **Teste 2** passou - IDs inválidos são filtrados
- [ ] **Teste 3** passou - Request vazio é rejeitado
- [ ] **Teste 6** passou - Guid.Empty é detectado
- [ ] Logs aparecem corretamente no console do navegador
- [ ] Logs aparecem corretamente no backend (Application Insights/Console)
- [ ] Nenhum erro de compilação no backend
- [ ] Nenhum erro de TypeScript no frontend

---

## 🚀 Deploy

### Backend
```powershell
cd api-core
dotnet publish -c Release
# Deploy automático via GitHub Actions ou FTP manual
```

### Frontend
```powershell
cd crm-web
npm run build
# Deploy automático via GitHub Actions
```

---

## 📞 Próximos Passos se Erro Persistir

Se após estas mudanças o erro continuar:

1. **Coletar evidências completas:**
   - Screenshot dos logs do frontend (console)
   - Screenshot dos logs do backend
   - Resultado da query SQL verificando a receita

2. **Análise de caso específico:**
   - O ID `51955c50-3c0d-4282-ac33-1be06eeccf76` está correto?
   - O usuário logado é o dono da receita?
   - Há algum caractere invisível ou espaço no ID?

3. **Testes adicionais:**
   - Tentar excluir unitariamente (DELETE /incomes/{id})
   - Verificar se módulo de despesas tem mesmo problema
   - Testar com receita recém-criada

---

**Data de Criação:** 06/02/2026
**Versão:** 1.0
**Responsável:** Correção de Bug - Bulk Delete Incomes
