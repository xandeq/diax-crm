# 🔍 Guia de Teste Pós-Deploy - Bulk Delete Incomes & Expenses

## ✅ Commits Realizados

**Commit 1:** `65f3892` - Correção para Incomes
**Commit 2:** `9b4b781` - Correção para Expenses

---

## ⏳ Aguardar Deploy

Os pipelines de CI/CD foram acionados. Aguarde ~5-10 minutos para conclusão:
- ✅ `.github/workflows/deploy-api-core-smarterasp.yml`
- ✅ `.github/workflows/deploy-crm-web-hostgator.yml`

Verifique em: https://github.com/xandeq/diax-crm/actions

---

## 🧪 Teste 1: Verificar se Novo Código foi Deployado

### Frontend (Console do Navegador)

1. Abrir https://crm.alexandrequeiroz.com.br/finance/incomes
2. **CTRL + F5** (hard refresh para limpar cache)
3. Abrir DevTools (F12) → Console
4. Selecionar 1 receita qualquer
5. Clicar em "Excluir Selecionados"

**✅ Logs Esperados no Console:**
```
🔍 [Incomes] Enviando 1 IDs para exclusão: ["guid-aqui"]
```

Se aparecer `⚠️ ID inválido filtrado:` → **PROBLEMA IDENTIFICADO!**

### Backend (Application Insights ou Logs)

**✅ Logs Esperados:**
```
[Information] [BulkDelete] Iniciado com 1 IDs
[Debug] [BulkDelete] IDs recebidos: guid-aqui
[Debug] [BulkDelete] UserId resolvido: {guid}
[Information] [IncomeService.DeleteRangeAsync] Iniciando exclusão de 1 receitas para usuário {guid}
```

Se NÃO aparecerem → Deploy ainda não concluído ou código antigo em cache.

---

## 🧪 Teste 2: Testar com Despesas

1. Abrir https://crm.alexandrequeiroz.com.br/finance/expenses
2. **CTRL + F5**
3. Selecionar 1 despesa
4. Clicar em "Excluir Selecionados"

**✅ Console do Navegador:**
```
🔍 [Expenses] Enviando 1 IDs para exclusão: ["guid-aqui"]
```

**✅ Backend:**
```
[Information] [BulkDelete] Iniciado com 1 IDs
[Information] [ExpenseService.DeleteRangeAsync] Iniciando exclusão de 1 despesas para usuário {guid}
```

---

## 🔍 Diagnóstico de Erros (Se Continuar Falhando)

### Cenário A: Logs Aparecem mas Erro Persiste

Se você vir logs tipo:
```
[IncomeService.DeleteAsync] ERRO: Receita {guid} existe mas pertence ao usuário {outro-user-id}, tentativa de exclusão por {seu-user-id}
```

**Causa:** Problema de multitenancy/autenticação.
**Solução:** Verificar resolução de `UserId` no token JWT.

---

### Cenário B: Logs Não Aparecem

Se nenhum log novo aparecer:

1. **Verificar versão deployada:**
   ```bash
   curl https://api.alexandrequeiroz.com.br/health
   ```

2. **Limpar cache do navegador:**
   - CTRL + SHIFT + DELETE
   - Selecionar "Cache" e "Cookies"
   - Período: "Última hora"

3. **Verificar GitHub Actions:**
   - Ir para: https://github.com/xandeq/diax-crm/actions
   - Verificar se workflows terminaram com sucesso

---

### Cenário C: IDs Inválidos Detectados

Se você vir:
```
⚠️ [Incomes] ID inválido filtrado: 0
⚠️ [Incomes] ID inválido filtrado: 1
⚠️ [Incomes] ID inválido filtrado: 2
```

**Causa:** `selectedIds` está usando índices da tabela ao invés de IDs reais!
**Solução:** Problema no `rowSelection` da tabela.

**Debug adicional:**
```javascript
// No console do navegador, antes de excluir:
console.log('Row Selection:', table.getState().rowSelection);
console.log('Selected IDs:', selectedIds);
```

Se `selectedIds` for `["0", "1", "2"]` ao invés de GUIDs válidos, precisamos ajustar o componente da tabela.

---

## 📊 Coletar Evidências Completas

Se o erro continuar, colete:

### 1. Console do Navegador
```javascript
// Executar no console antes de testar:
console.log('=== TESTE BULK DELETE ===');
console.log('User:', localStorage.getItem('user'));
console.log('Token:', localStorage.getItem('token'));

// Tentar exclusão e capturar erro completo:
try {
    await financeService.deleteIncomesBulk(['51955c50-3c0d-4282-ac33-1be06eeccf76']);
} catch (err) {
    console.error('Erro completo:', err);
    console.error('Response:', err.response);
}
```

### 2. Network Tab
1. Abrir DevTools → Network
2. Filtrar por "bulk-delete"
3. Tentar exclusão
4. Clicar na requisição
5. Copiar:
   - **Request Headers** (especialmente Authorization)
   - **Request Payload**
   - **Response** (completo)

### 3. Backend Logs

Acessar Application Insights ou logs da SmarterASP e procurar por:
- `[BulkDelete]`
- `[IncomeService]` ou `[ExpenseService]`
- Horário: últimos 5 minutos

---

## 🎯 Checklist de Validação

Após deploy completo:

- [ ] **Frontend limpo:** CTRL + F5 executado
- [ ] **Console logs aparecem:** `🔍 Enviando X IDs...`
- [ ] **Backend logs aparecem:** `[BulkDelete] Iniciado...`
- [ ] **IDs são GUIDs válidos:** Não são `["0", "1", "2"]`
- [ ] **Exclusão funciona:** Receita/Despesa removida com sucesso
- [ ] **Erro específico:** Se falhar, mensagem indica causa raiz

---

## 📞 Se Precisar de Mais Ajuda

Envie:
1. Screenshot do console do navegador (com logs completos)
2. Screenshot do Network Tab (payload + response)
3. Logs do backend (últimos 5 minutos)
4. Responda:
   - Deploy completou? (verificar GitHub Actions)
   - CTRL + F5 foi executado?
   - IDs no console são GUIDs válidos ou números?

---

**Próximo Deploy:** ~5-10 minutos após push
**Data/Hora do Push:** 06/02/2026 20:15
**Commits:** 65f3892 (Incomes) + 9b4b781 (Expenses)
