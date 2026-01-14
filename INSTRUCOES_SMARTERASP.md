# Instruções de Configuração no SmarterASP

## ⚠️ Problema: Endpoint /auth/me retorna 401 após login

### Causa
A chave JWT não está configurada em produção, fazendo com que a aplicação use chaves efêmeras em memória. A cada restart da aplicação, novas chaves são geradas e tokens antigos se tornam inválidos.

---

## ✅ Solução

### 1. Configurar Variável de Ambiente JWT no Painel SmarterASP

**Passos:**

1. Acesse o painel de controle SmarterASP: https://member5-3.smarterasp.net/cp/
2. Navegue para: **Site Settings** → **ASP.NET Core Settings**
3. Na seção **Environment Variables**, adicione:

| Nome | Valor |
|------|-------|
| `DIAX_Jwt__Key` | `SUA_CHAVE_SECRETA_AQUI_COM_PELO_MENOS_32_CARACTERES_EXEMPLO_DiaxCRM2026SecureProductionKey987654321` |

**⚠️ IMPORTANTE:**
- A chave deve ter **no mínimo 32 caracteres**
- Use caracteres alfanuméricos aleatórios
- **NÃO compartilhe esta chave** em repositórios públicos
- Recomendamos usar um gerador de senhas forte

**Exemplo de chave segura:**
```
DiaxCRM_P8oD3f!mK2#qR9xV@wL5nT7jH4bN6gC1eA0yS
```

4. Clique em **Save** ou **Apply**

---

### 2. Criar Diretório para Data Protection Keys

**Via FTP ou Painel de Arquivos:**

1. Conecte-se ao servidor via FTP ou File Manager do painel
2. Navegue até: `h:\root\home\partiurock-003\www\api-diax-crm\`
3. Crie a estrutura de diretórios:
   ```
   App_Data/
   └── keys/
   ```

**Permissões:**
- O diretório `App_Data/keys` deve ter permissão de escrita para o processo IIS
- No SmarterASP, isso geralmente é configurado automaticamente

---

### 3. Reiniciar a Aplicação

**Opção 1: Via Painel**
1. Vá para **Site Settings** → **Application Pool**
2. Clique em **Recycle**

**Opção 2: Via FTP**
1. Edite o arquivo `web.config` (qualquer alteração, até adicionar um espaço)
2. Salve o arquivo - isso força o IIS a reiniciar a aplicação

**Opção 3: Via Deploy**
1. Faça push das alterações no GitHub
2. Aguarde o GitHub Actions fazer o deploy automaticamente

---

### 4. Verificar se o Problema foi Resolvido

**Teste o fluxo completo:**

1. **POST** `https://api-diax-crm.partiurock.com/api/v1/auth/login`
   ```json
   {
     "email": "seu-email@exemplo.com",
     "password": "sua-senha"
   }
   ```
   - ✅ Deve retornar **200 OK** com token JWT

2. **GET** `https://api-diax-crm.partiurock.com/api/v1/auth/me`
   - Header: `Authorization: Bearer SEU_TOKEN_JWT_AQUI`
   - ✅ Deve retornar **200 OK** com dados do usuário
   - ❌ Se ainda retornar **401**, verifique os logs

---

### 5. Verificar Logs

**Caminho dos logs:** `h:\root\home\partiurock-003\www\api-diax-crm\logs\`

**Logs esperados após configuração:**
```
[INF] Data Protection configurado para persistir chaves em: h:\root\home\partiurock-003\www\api-diax-crm\App_Data\keys
[INF] JWT key configured from environment variable.
```

**⚠️ Warnings que devem DESAPARECER:**
```
[WRN] Using an in-memory repository. Keys will not be persisted to storage.
[WRN] Neither user profile nor HKLM registry available. Using an ephemeral key repository.
```

---

## 🔐 Segurança Adicional

### Opções de Configuração JWT

Você pode configurar as seguintes variáveis de ambiente adicionais:

| Variável | Padrão | Descrição |
|----------|--------|-----------|
| `DIAX_Jwt__Key` | *(obrigatório)* | Chave secreta para assinar tokens JWT |
| `DIAX_Jwt__Issuer` | `DiaxCRM` | Emissor do token |
| `DIAX_Jwt__Audience` | `DiaxCRM` | Audiência do token |
| `DIAX_Jwt__ExpirationInMinutes` | `60` | Tempo de expiração do token em minutos |

---

## 📋 Checklist de Deploy

- [ ] Configurar `DIAX_Jwt__Key` no painel SmarterASP
- [ ] Criar diretório `App_Data/keys` no servidor
- [ ] Fazer deploy das alterações no código via GitHub Actions
- [ ] Reiniciar a aplicação
- [ ] Testar login (`POST /api/v1/auth/login`)
- [ ] Testar autenticação (`GET /api/v1/auth/me`)
- [ ] Verificar logs para confirmar ausência dos warnings

---

## 🆘 Troubleshooting

### Ainda recebe 401 após login?

1. **Verifique se a variável de ambiente foi salva:**
   - Vá em ASP.NET Core Settings e confirme que `DIAX_Jwt__Key` está listada

2. **Verifique os logs:**
   - Procure por `[ERR]` ou `[WRN]` relacionados a JWT
   - Confirme se a mensagem "Data Protection configurado" aparece

3. **Reinicie novamente a aplicação:**
   - Às vezes é necessário reciclar o Application Pool duas vezes

4. **Verifique se o token não expirou:**
   - Tokens têm validade de 60 minutos por padrão
   - Faça um novo login para obter um token válido

5. **Verifique o formato do token:**
   - O token deve estar no formato: `Authorization: Bearer eyJhbGc...`
   - Não deve ter aspas ou espaços extras

---

## 📞 Suporte

Se o problema persistir:
1. Copie os logs completos de `h:\root\home\partiurock-003\www\api-diax-crm\logs\`
2. Verifique as variáveis de ambiente configuradas
3. Confirme que o diretório `App_Data/keys` existe e tem permissão de escrita
