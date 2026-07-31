# 🏛️ Módulo: Patrimônio & Investimentos

> **Onde vive:** dentro do **Módulo Financeiro** do DIAX CRM, como nova seção **abaixo da
> "Planilha Financeira"** (`/finance/personal-control`).
> **Rota nova:** `/finance/patrimonio` · **Nav:** item logo após "Planilha Financeira" em `FinanceNav.tsx`.
> **Titulares:** Alexandre **+ esposa** (patrimônio conjunto — `Ownership: Alexandre | Esposa | Conjunto`).
> **Integração:** InvestIQ já conectado via `InvestIQController` (`/planner/investiq/portfolio-summary`).
> Data: 2026-07-30

---

## 0. Objetivo (nas palavras do Alexandre)

Área para **controlar o patrimônio** (bens móveis, imóveis, investimentos) **e** receber
**todo dia** recomendações / dicas / opções de **qualquer forma de investimento** para
Alexandre + esposa **adquirirem patrimônio**. Não é só carteira de bolsa — é wealth cockpit
+ copiloto de aquisição.

**Classes cobertas:** ouro · diamante/pedras · ações · dividendos · fundos · FIIs · renda fixa
· fundos multimercado · automóvel · imóvel (apto/casa/terreno) · consórcio · dólar/libra ·
ativos no exterior · milhas · cripto.

---

## 1. Snapshot F0 — patrimônio hoje

**Titulares:** Alexandre + esposa.

### Ativos próprios (no nome hoje)
| Ativo | Classe | Valor R$ | Liquidez | Valuation | Status |
|---|---|---|---|---|---|
| Toyota Corolla Cross | veículo | ~180.000 *(est.)* | ilíquido | FIPE (deprec.) | ⚠ confirmar ano/versão |
| Consórcio Porto Seguro (terreno, carta R$140k) | consórcio | = valor aportado | travado | extrato Porto | ⚠ confirmar parcelas pagas / aportado / parcela |

> Consórcio: patrimônio hoje = só o aportado ("no início"), **não** a carta de R$140k.
> Parcelas futuras = compromisso mensal (passivo de fluxo).

### A receber / contingente (herança do pai — só na venda, sem data)
| Item | Bruto R$ | Parte Alexandre | Obs |
|---|---|---|---|
| Casa de praia — Guarapari | 750.000 | **250.000** (1/3) | não vendida |
| Terreno (Minas?) | 180.000 | 60.000 (1/3) | não vendido |
| + crédito vs irmão "Dole" no terreno | — | **~60.000** | irmão deve → ~R$120k total no terreno |
| **Total contingente** | | **~370.000** | fora do net worth realizável |

### Net worth
| Camada | R$ |
|---|---|
| Líquido investível (ações/FII/RF/cripto/caixa aplicado) | **X — confirmar (lacuna #3)** |
| Ilíquido próprio (carro + consórcio) | ~180.000 + consórcio |
| **Patrimônio próprio realizável** | **~180.000 + X** |
| Contingente (herança) | ~370.000 |
| **Patrimônio projetado** | **~550.000 + X** |

### Lacunas a confirmar
1. Corolla Cross: ano + versão → FIPE exata.
2. Consórcio Porto: parcelas pagas + total aportado + parcela mensal.
3. **InvestIQ tem 33 posições "reais" (16 ações + 14 FIIs + 3 RF) no DB — são de verdade hoje,
   ou demo/já vendidas?** Muda `X` e o net worth inteiro.

---

## 2. Onde encaixa no CRM (placement técnico)

### Frontend (`crm-web`, Next.js App Router)
```
src/app/finance/patrimonio/           # NOVO
├── page.tsx                          # Net worth + alocação + oportunidades do dia
├── assets/                           # CRUD de bens/ativos (todas as classes)
└── opportunities/                    # Feed diário de recomendações
```
- `FinanceNav.tsx`: novo item `{ name: 'Patrimônio & Investimentos', href: '/finance/patrimonio' }`
  inserido **logo após** "Planilha Financeira".
- `DashboardClient.tsx`: card novo na grade de atalhos do financeiro.
- `src/services/patrimonio.ts`: DTOs + fetch (espelha padrão de `finance.ts`).

### Backend (`api-core`, .NET 8, DDD)
```
Diax.Domain/Finance/Assets/           # NOVO
├── Asset.cs                          # bem/investimento (rich model)
├── AssetClass.cs (enum)             # Ouro|Diamante|Acao|Fii|RendaFixa|Fundo|Multimercado|
│                                     #   Veiculo|Imovel|Consorcio|Moeda|Exterior|Milhas|Cripto
├── AssetLiquidity.cs (enum)         # Liquido|Iliquido|Travado|Contingente
├── AssetOwnership.cs (enum)         # Alexandre|Esposa|Conjunto
├── AssetValuation.cs                # histórico de valor (série temporal)
├── InvestmentOpportunity.cs         # oportunidade/dica do dia (classe, tese, score, fonte)
└── IAssetRepository.cs
Diax.Application/Finance/Patrimonio/  # AssetService, OpportunityService, NetWorthService
Diax.Api/Controllers/V1/PatrimonioController.cs   # NOVO (reusa InvestIQController p/ mercado)
api-core/migrations/                  # migration nova (assets, asset_valuations, opportunities)
```
- Reusa `IUserOwnedEntity` (multi-tenant) + `IUnitOfWork` + Global Query Filter por `UserId`.
- `Asset` é sibling de `FinancialAccount` (que já tem `AccountType = Investimento`).

---

## 3. Motor de recomendações diárias (o núcleo)

**Roda 1x/dia** (cron/routine, igual ao morning-briefing 8h30 já existente) e gera um feed de
oportunidades por classe. LLM usa o **free pool** (`AI_FORCE_FREE`, já no InvestIQ) — **sem
geração paga sem permissão**.

### Fontes de dados por classe
| Classe | Fonte | Tipo |
|---|---|---|
| Ações / FII / RF / Fundos BR | **InvestIQ** (engine já existe) | auto/live |
| Cripto | CoinGecko (free) | auto/live |
| Dólar / Libra | AwesomeAPI / exchangerate (free) | auto/live |
| Ouro | spot gold API (free tier) | auto/live |
| Multimercado / Fundos | dados CVM | auto |
| Imóvel (apto/casa/terreno) | índice FipeZap / curado | semi (sem quote realtime) |
| Consórcio | curado / manual | qualitativo |
| Diamante / pedras | curado / manual | qualitativo (sem mercado líquido) |
| Milhas | promoções Livelo/Smiles (curado) | qualitativo |
| Exterior | ETFs/brokers (via InvestIQ ou curado) | semi |

> Honestidade: diamante, milhas, consórcio e imóvel **não têm cotação em tempo real**.
> Nessas classes o "diário" é **dica curada + LLM**, não quote — sinalizado na UI como tal.

### Pipeline diário
```
1. Coleta multi-fonte (InvestIQ + APIs free por classe)
2. Lê contexto do usuário: surplus mensal (CRM) + net worth + perfil de risco + metas
3. LLM (free pool) rankeia → N oportunidades/classe com: tese, score, valor sugerido de aporte,
   ligação ao surplus disponível
4. Persiste InvestmentOpportunity do dia + entrega no /finance/patrimonio e no morning-briefing
```

### Integração InvestIQ (interligação por dentro)
- **Já consumido:** `GET /planner/investiq/portfolio-summary` (via `InvestIQController`).
- **Adicionar no InvestIQ (FastAPI):** `GET /opportunities/daily` → CRM consome no motor.
- InvestIQ continua sendo o **cérebro de mercado**; CRM é a **casa do patrimônio + copiloto**.

---

## 4. Fases

| Fase | Entrega | Status |
|---|---|---|
| **F0** | Snapshot patrimônio (este doc) | ✅ (faltam 3 lacunas) |
| **F1** | Domain `Asset` + migration + `PatrimonioController` + CRUD + net worth view em `/finance/patrimonio` | ⏳ |
| **F2** | Motor diário de recomendações (multi-fonte + LLM free pool) + feed na UI + morning-briefing | ⏳ |
| **F3** | Auto-valuation (mercado via InvestIQ; físicos por deprec./índice) + alertas + rebalanceamento | ⏳ |

**Protocolo:** cada fase com código significativo → rodar `/wave-qa` antes de fechar (migration
head → smoke API → e2e → regressão → commit). Push só com autorização (auto-deploy via GitHub Actions).

---

## 5. Layout da página `/finance/patrimonio` (3 zonas)

1. **Meu Patrimônio** (o que tenho) — net worth realizável/contingente/projetado + alocação + tabela CRUD. [F1]
2. **Onde investir** (o que posso + onde recomenda) — feed diário de oportunidades por classe,
   rankeado por facilidade (moeda→RF→ações→ouro→...) + fit ao surplus/risco. Card: classe · tese
   · valor sugerido de aporte · score · fonte. [F1 stub → F2]
3. **Ações a tomar** (next best actions) — checklist do gap alocação-atual × alvo + surplus do CRM.
   Cada ação pode virar **task no CRM**. [F1 stub → F2]

## 6. Design (polish `/impeccable`, após F1 funcional)

- Estilo: **moderno, intuitivo, criativo** — sobe primeiro na cara do CRM (Shadcn/Tailwind), polish depois.
- **Contraste theme-aware obrigatório**: fundo escuro → texto/cor clara; fundo claro → texto/cor escura.
  Mínimo WCAG AA; usar tokens que adaptam nos modos dark/light do CRM (nunca cor fixa que some no outro tema).

## 7. Priorização de classes (facilidade de adquirir patrimônio — ordem-padrão do motor F2)

1 Moeda forte aplicada (USD/GBP/EUR) · 2 Títulos/RF · 3 Ações c/ dividendos · 4 Ouro ·
5 FGTS (fonte de capital, não ativo) · 6 Consórcio + Carta de Crédito · 7 Imóvel ·
8 Importar · 9 Dropshipping · 10 Automóvel (deprecia) · 11 Diamantes (baixa liquidez).

**Classes nomeadas a adicionar pós-F1 (aditivo):** `Fgts`, `Titulo`, `CartaCredito`, `Negocio` (export/import/dropshipping).
Negócios/operações = balde separado (geram renda, não parkeiam valor). FGTS entra no planejador de aporte, fora do net worth realizável.
