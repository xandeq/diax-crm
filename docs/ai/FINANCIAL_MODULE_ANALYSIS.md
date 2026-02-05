# Análise Arquitetural - Módulo Financeiro (DIAX CRM)

**Data da Análise:** 04/02/2026
**Auditor:** GitHub Copilot (Agente Arquitetura)
**Versão do Sistema:** .NET 8 (Backend) / Next.js 14 (Frontend)

---

## 1. Resumo Executivo

O Módulo Financeiro do DIAX CRM apresenta uma maturidade estrutural avançada, seguindo fielmente os princípios da **Clean Architecture**. O núcleo de domínio está bem isolado, e as regras de negócio complexas (como importação de extratos) estão encapsuladas na camada de Aplicação.

**Pontos Fortes:**
- Separação clara entre *Cash Flow* (Contas) e *Accrual Basis* (Cartões de Crédito).
- Implementação robusta do padrão **Staging** para importações de dados (Extratos).
- Modelagem rica de domínio com uso extensivo de Enums e Value Objects.

**Pontos de Atenção:**
- Performance em relatórios (filtragem em memória detectada).
- Complexidade na conciliação de faturas de cartão de crédito.

---

## 2. Modelagem de Domínio (`Diax.Domain.Finance`)

O coração do sistema reside em `api-core/src/Diax.Domain/Finance`. A modelagem não é apenas CRUD, refletindo processos reais de contabilidade pessoal/empresarial.

### 2.1. Entidades Principais

| Entidade | Responsabilidade | Relacionamentos Chave |
| :--- | :--- | :--- |
| **`FinancialAccount`** | Representa o "caixa" real (Bancos, Carteiras). | Possui saldo (`Balance`). É a origem pagadora de `Expenses`. |
| **`CreditCard`** | Instrumento de crédito. Não possui saldo monetário direto, mas gera faturas. | Pertence a um `CreditCardGroup` (para limites compartilhados). |
| **`Expense`** | Unidade atômica de saída de valor. | Polimórfica: Vínculo com `FinancialAccount` (Débito) **OU** `CreditCardInvoice` (Fatura). |
| **`Income`** | Unidade atômica de entrada de valor. | Sempre vinculada a uma `FinancialAccount` (destino). |
| **`CreditCardInvoice`** | Agregador de despesas de cartão. | Respeita o `ClosingDay` e `DueDay`. Transforma múltiplas `Expenses` em um único débito futuro. |

### 2.2. Fluxo de Transações

O sistema distingue elegantemente dois tipos de despesas via `PaymentMethod`:

1.  **Débito Imediato (Cash/Pix/Debit):**
    *   Afeta imediatamente o `Balance` da `FinancialAccount`.
    *   Status geralmente nasce como `Paid` ou `Pending` (agendado).
2.  **Crédito (CreditCard):**
    *   **Não** afeta a conta bancária no momento da compra.
    *   É anexada a uma `CreditCardInvoice` aberta.
    *   O impacto no caixa só ocorre quando a **Fatura** é paga.

---

## 3. Serviços de Aplicação (`Diax.Application.Finance`)

Esta camada orquestra os casos de uso. O destaque técnico é o pipeline de importação.

### 3.1. Pipeline de Importação (`StatementImportService`)

O sistema não insere dados "sujos" no banco. Ele usa um padrão de **Staging Area**:

1.  **Upload:** O arquivo (CSV/OFX/PDF) é recebido.
2.  **Parser Factory:** `IdentifyParser` seleciona a estratégia correta (`IFileParser`).
3.  **Transição:** Os dados brutos são convertidos em `ImportedTransaction` (Entidade temporária).
4.  **Conciliação (Postagem):**
    *   O usuário revisa os dados na pré-visualização.
    *   O sistema tenta deduzir duplicatas (hash de Data + Valor).
    *   Ao confirmar, transforma `ImportedTransaction` em `Expense` ou `Income`.

### 3.2. Relatórios e Dashboard (`FinancialSummaryService`)

Calcula indicadores vitais para saúde financeira:
*   **NetCashFlow (Caixa Líquido):** Receitas - Despesas Pagas. (O que realmente sobrou).
*   **ProjectedCashFlow (Projetado):** Receitas - (Despesas Pagas + Pendentes). (Previsão de fim de mês).

---

## 4. Análise Técnica e Code Review

### 4.1. Qualidade do Código

O código em `Expense.cs` demonstra boas práticas de **Domain-Driven Design (DDD)**:
*   **Encapsulamento:** Setters privados (`private set`), obrigando o uso de Construtores ou Métodos de Fábrica.
*   **Validação de Invariantes:** O construtor valida regras básicas (ex: `amount > 0`, `description` não nula).
*   **Riqueza Semântica:** Uso de Enums (`PaymentMethod`, `ExpenseStatus`) ao invés de strings mágicas.

### 4.2. Pontos Críticos (Tech Debt & Riscos)

#### 🔴 Risco de Performance: `FinancialSummaryService`
No método `GetSummaryAsync`, o código realiza:
```csharp
var allExpenses = await _expenseRepository.GetAllAsync(cancellationToken); // TRAZ TUDO DO BANCO
var expensesInPeriod = allExpenses
    .Where(e => e.Date >= startDate && e.Date <= endDate) // FILTRA EM MEMÓRIA
    .ToList();
```
*   **Problema:** Conforme o banco crescer (milhares de transações), essa query vai estourar a memória (OOM) e travar o banco.
*   **Solução:** O filtro de data **DEVE** ser passado para o Repositório (`IExpenseRepository.GetByPeriodAsync(...)`) para gerar um `WHERE` no SQL.

#### 🟡 Regra de Negócio: Fatura de Cartão
A lógica de fechar faturas (`ClosingDay`) é complexa. Se o usuário mudar o dia de vencimento do cartão, o sistema precisa recalcular faturas abertas ou manter o histórico? Atualmente, essa lógica parece residir parcialmente no Frontend ou distribuída, podendo gerar inconsistências.

---

## 5. Integração Frontend (`crm-web`)

O Frontend espelha fielmente os contratos do Backend.

- **Type Safety:** Os Enums em `src/services/finance.ts` (`PaymentMethod`, `AccountType`) estão perfeitamente sincronizados com o C#.
- **Abstração de API:** O uso de `apiFetch` centraliza o tratamento de erros e autenticação, evitando `fetch` disperso nos componentes.
- **UX de Importação:** A separação da UI de importação (Upload -> Preview -> Confirmar) é excelente para evitar erros de entrada de dados em massa.

---

## 6. Recomendações do Arquiteto

1.  **Refatoração Imediata (P1):** Alterar `FinancialSummaryService` para usar consultas filtradas no banco de dados (SQL WHERE) ao invés de filtrar em memória (LINQ to Objects).
2.  **Automação (P2):** Implementar "Regras de Importação Inteligente". Ex: Se a descrição contém "Uber", categorizar automaticamente com "Transporte". Hoje o processo parece ser 1-para-1 manual ou simplificado.
3.  **Auditabilidade (P3):** Garantir que alterações de saldo cruciais (como exclusão de transação conciliada) gerem logs em `AuditableEntity` detalhados ou uma tabela de histórico ledger imutável.

---

**Conclusão:** O módulo é sólido, bem arquitetado e pronto para escala, exigindo apenas ajustes pontuais de performance nas queries de relatório.
