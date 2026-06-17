# Checklist de Deliverability e DNS — DIAX CRM

Antes de disparar qualquer e-mail marketing ou campanha de prospecção fria (*outreach*) utilizando o DIAX CRM, siga este guia passo a passo para garantir que suas mensagens cheguem à caixa de entrada principal dos leads, evitando filtros de spam corporativos.

---

## 1. Escolha e Registro de Domínio Secundário
*Nunca dispare prospecção fria pelo seu domínio corporativo principal.* Se o domínio principal for sinalizado como spam, toda a comunicação da sua empresa (e-mails com clientes, fornecedores e parceiros) será comprometida.

* **Recomendação**: Adquira domínios alternativos semelhantes ao principal.
  * *Domínio Principal*: `diaxcrm.com.br`
  * *Domínios Sugeridos para Outbound*: `mail-diaxcrm.com.br`, `go-diaxcrm.com.br` ou `news-diaxcrm.com.br`.

---

## 2. Configurações DNS Obrigatórias
No painel do seu registrador de domínio secundário, configure os seguintes registros para autorizar o envio em nome da sua conta:

### SPF (Sender Policy Framework)
Define quais servidores têm autorização para enviar e-mails em nome do seu domínio.
* **Tipo**: TXT
* **Nome/Host**: `@`
* **Valor (Exemplo para Brevo)**: `v=spf1 include:spf.sendinblue.com ~all`
* *Nota*: Ajuste o include de acordo com o SMTP contratado (ex: Resend, Mailjet, SendGrid).

### DKIM (DomainKeys Identified Mail)
Garante uma assinatura criptográfica de autenticidade em cada mensagem disparada.
* **Tipo**: TXT
* **Nome/Host**: `mail._domainkey` (ou o seletor fornecido pelo SMTP)
* **Valor**: [Chave pública fornecida nas configurações do seu provedor de e-mail]

### DMARC (Domain-based Message Authentication)
Informa aos provedores de destino (Gmail, Outlook) como agir caso um e-mail falhe no SPF/DKIM.
* **Tipo**: TXT
* **Nome/Host**: `_dmarc`
* **Valor sugerido (Modo Suave / Monitoramento)**: `v=DMARC1; p=none; pct=100; rua=mailto:postmaster@seudominio.com.br`
* **Valor sugerido (Modo Rígido - pós aquecimento)**: `v=DMARC1; p=quarantine; pct=100;`

### Custom Tracking Domain (Domínio Personalizado de Rastreamento)
Garante que os links de rastreamento de clique no DIAX CRM apontem para o seu domínio e não para um domínio genérico compartilhado com outras contas em blacklists.
* **Tipo**: CNAME
* **Nome/Host**: `click` ou `track`
* **Valor**: Apontando para o endereço de rastreamento do DIAX CRM (ex: `track.diaxcrm.com.br`).

### Return-Path (Alinhamento de SPF)
Garante que os bounces (erros de entrega) sejam direcionados e processados pelo seu provedor SMTP de forma alinhada.
* Geralmente configurado pelo provedor de SMTP ao realizar a autenticação do subdomínio (ex: via registros MX ou CNAME). Certifique-se de que o Return-Path aponta para o domínio secundário (ex: `mail.diaxcrm.com.br`) para manter o alinhamento de SPF.

### Reply-To (Caixa de Resposta Real)
* Configure o endereço de `reply-to` para uma caixa postal corporativa real e monitorada (geralmente no domínio principal, ex: `vendas@diaxcrm.com.br` ou o e-mail do próprio vendedor).
* Nunca use endereços `no-reply@` em campanhas cold email. Respostas de leads sinalizam engajamento positivo para os servidores de spam (Gmail/Outlook), elevando sua reputação de disparo.

---

## 3. Estratégia de Warm-up (Aquecimento de Domínio)
Provedores de e-mail desconfiam de domínios novos que enviam dezenas de mensagens repentinamente. O aquecimento gradual é fundamental nas primeiras 4 semanas de vida do domínio secundário.

| Semana | Limite de Envios Diários | Tipo de Envio | Ação Obrigatória |
| :--- | :--- | :--- | :--- |
| **Semana 1** | 10 a 15 e-mails / dia | Manual / Testes controlados | Responder e marcar como favorito em contas pessoais. |
| **Semana 2** | 20 a 30 e-mails / dia | Automação piloto (DIAX CRM) | Foco nos leads de maior qualidade. |
| **Semana 3** | 40 a 50 e-mails / dia | Automação gradual | Monitorar taxas de rejeição (*bounces*). |
| **Semana 4** | 80 a 100 e-mails / dia | Produção controlada | Manter o teto recomendado de 100/dia por caixa. |

---

## 4. Regras Gerais de Entregabilidade
* **Filtro de Opt-out / Unsubscribe**: Todo e-mail outbound deve conter a variável `{{unsubscribe_url}}`. Se o lead clicar em descadastro, o `BrevoWebhookController` registrará e impedirá automaticamente novos envios.
* **Tamanho do Arquivo**: O HTML do template de e-mail deve pesar menos de **100 KB** (Idealmente menos de 30 KB). Acima disso, o Gmail costuma truncar a mensagem.
* **Quantidade de Links**: Use no máximo **2 links** por email (um para a landing page e um para descadastro). Excesso de links aciona alarmes de spam.
* **Proporção Texto vs Imagem**: Evite emails constituídos exclusivamente de imagens. Caso use imagens de suporte, sempre inclua texto descritivo correspondente e mantenha 80% do corpo composto por texto puro.
* **Bounce Threshold (Limite de Rejeição)**: Se a taxa de bounces permanentes da sua lista ultrapassar **1%**, a campanha deve ser pausada no DIAX CRM para revisão de higienização de lista.
* **Plain Text Fallback**: Sempre envie a versão em texto puro acoplada ao HTML (o worker de email do CRM faz esse processo automaticamente).

---

## 5. Lista de Palavras Suspeitas (Spam Words)
Evite incluir estes termos no assunto ou no início do corpo do e-mail:

* *Comercial*: promoção, grátis, barato, desconto, clique aqui, compre agora, ganhe dinheiro, urgente.
* *Monetário*: R$, $, reais, dólares, faturamento fácil, lucros.
* *Pontuação*: Excesso de exclamações (!!!), letras em caixa alta (GELADEIRA GRÁTIS).
