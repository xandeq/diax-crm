# Phase 7: Extração — Qualidade na Entrada - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-09-05
**Phase:** 07-extra-o-qualidade-na-entrada
**Areas discussed:** Checagem de MX em .NET, Registro do motivo de rejeição, Persistência da
classificação site-vs-diretório, Destino da ponte Python

---

## Checagem de MX em .NET

| Option | Description | Selected |
|--------|-------------|----------|
| DnsClient.NET | Lib NuGet consolidada, query MX de verdade. +1 dependência, mas SmarterASP/IIS pode bloquear Process.Start | ✓ |
| Shell-out p/ nslookup | Mesma abordagem do Python. Zero dependência NuGet, mas Process.Start em shared hosting é frágil | |
| Só fallback A/AAAA | System.Net.Dns.GetHostEntry, sem lib nova. Mais fraco que MX de verdade | |

**User's choice:** DnsClient.NET
**Notes:** .NET não tem lookup de MX nativo — `System.Net.Dns` só resolve A/AAAA/PTR. A escolha
prioriza confiabilidade em produção sobre menor número de dependências.

### Follow-up: comportamento em falha de DNS

| Option | Description | Selected |
|--------|-------------|----------|
| Deixa passar + loga | Falha de infra não é prova de lead ruim; registra "MX não verificado" | ✓ |
| Rejeita (estrito) | Mesma regra do geo: não verificável = fora | |
| Tenta de novo depois | Marca pendente, re-tenta no próximo ciclo (precisa de estado) | |

**User's choice:** Deixa passar + loga
**Notes:** Distinção explícita em relação ao filtro geográfico — lá o dado ausente é do lead;
aqui a ausência é da infraestrutura de DNS.

### Follow-up: cache

| Option | Description | Selected |
|--------|-------------|----------|
| Cache persistente c/ TTL | Espelha o mx-cache.json (30 dias) do Python | ✓ |
| Só em memória, por rodada | Simples, mas re-consulta ~1000 domínios/dia | |
| Sem cache | Mais simples; custo de latência por lead em todo import | |

**User's choice:** Cache persistente c/ TTL

---

## Registro do motivo de rejeição

| Option | Description | Selected |
|--------|-------------|----------|
| Agregado no CustomerImport | Reusa entidade existente; contadores por motivo por rodada; custo ~zero | ✓ |
| Tabela nova por lead | Auditoria completa, mas ~900 linhas/dia num banco a 93% da quota | |
| Agregado + amostra por lead | Contadores + N primeiros de cada motivo como amostra | |

**User's choice:** Agregado no CustomerImport
**Notes:** Constraint apresentada na pergunta: banco em 467/500 MB (audit_logs = 279 MB) e ~900
leads rejeitados/dia. Trade-off aceito conscientemente: perde-se o detalhe por lead.

---

## Persistência da classificação site-vs-diretório

| Option | Description | Selected |
|--------|-------------|----------|
| Coluna nova, junto c/ a da Phase 8 | `Customer.WebsiteKind`; migration única com o `ExternalId` da Phase 8 | ✓ |
| Coluna nova, migration só da Phase 7 | Entrega antes, mas duas migrations para coordenar | |
| No campo Tags | Zero migration, mas Tags já é texto livre sujo; consulta vira LIKE | |

**User's choice:** Coluna nova, junto c/ a da Phase 8
**Notes:** Decisão informada pelo risco real registrado no STATE.md — migration
`AddAgentFoundation` do v1.2 aplicada em produção com commits não pushados. Consolidar em uma
migration reduz pontos de coordenação de dois para um.

---

## Destino da ponte Python

| Option | Description | Selected |
|--------|-------------|----------|
| Aposentar após validar o worker | Worker vira fonte única; ponte fica como fallback documentado | ✓ |
| Manter os dois com config compartilhada | Python consome configs via API do CRM; lógica ainda em 2 lugares | |
| Manter os dois, auditar periodicamente | Status quo; foi assim que o `.ch` passou por um caminho e não pelo outro | |

**User's choice:** Aposentar após validar o worker
**Notes:** Dependência explícita registrada como D-10 — a aposentadoria só pode acontecer depois
de confirmar que o `ExtractorPullWorker` importa de fato em produção (primeira execução real
estava sendo verificada no momento desta discussão).

## Claude's Discretion

- Forma exata do cache de MX (tabela vs arquivo vs IMemoryCache + persistência)
- Timeout e política de retry da query DNS
- Serialização dos contadores por motivo no `CustomerImport`
- Paralelismo/batching das queries DNS durante o import

## Deferred Ideas

- Google Places API como fonte alternativa (EXTR-04, já em v2)
- Backfill dos leads já no banco que falhariam nos novos filtros — limpeza de dado existente,
  não qualidade de entrada
- Verificação SMTP real (handshake) — risco de bloqueio/reputação, fora de escopo
