# Revisão do plano — Phase 19 (Evento do Bot + Catálogo)

**Revisor:** gsd-pm, 2026-10-24. Plano fonte: `docs/plans/2026-10-23-fechar-venda.md`, Task 11.
Código real lido no worktree `_worktrees/diax-gsd` (mesma base de `feat/service-packages`,
commit `5bc7b186`/origin-main).

Contexto: Tasks 1-11 no geral têm fidelidade muito alta com o código real — nomes de arquivo,
números de linha (`ProposalService.cs:13/21/72/106`, `DiaxDbContext.cs:75`,
`DependencyInjection.cs:274/559/64`, `Program.cs:353`) todos confirmados exatos ou a 1-2 linhas de
distância. Os dois achados abaixo são as únicas divergências reais encontradas nesta fase.

## Achado 1 (médio) — `ToDtoAsync` não existe; o mapper real é `ToDto` síncrono com assinatura diferente

`docs/plans/2026-10-23-fechar-venda.md`, Task 11, bloco `CreateBotDraftAsync` (linhas ~910 e ~936
do plano):

```csharp
if (open != null) return Result.Success(await ToDtoAsync(open, ct)); // 1 rascunho aberto por lead
...
return Result.Success(await ToDtoAsync(proposal, ct));
```

Código real: `api-core/src/Diax.Application/Customers/ProposalService.cs:426`

```csharp
private static ProposalDto ToDto(Proposal p, string customerName) => new(...);
```

`ToDto` é **síncrono**, **privado**, e recebe `customerName: string` (não `CancellationToken`). O
padrão já usado em `CreateAsync` (linha 144) é carregar o customer e chamar
`ToDto(proposal, customer.Name)` — sem `await`. O próprio plano já previa essa incerteza ("se ele
for síncrono/privado com outro nome, reutilizar esse em vez de criar outro"), mas o nome sugerido
no código de exemplo (`ToDtoAsync`) não compila como está.

**Correção para quem executar a Task 11:** no ramo "já existe proposta aberta" e no final de
`CreateBotDraftAsync`, carregar o `Customer` (`await _customerRepository.GetByIdAsync(customerId, ct)`
ou reaproveitar o já carregado por `CreateAsync` quando disponível) e chamar
`ToDto(open, customer.Name)` / `ToDto(proposal, customer.Name)` sem `await`. Como `ToDto` é privado
na classe `ProposalService`, isso só funciona porque `CreateBotDraftAsync` é um método da própria
classe — nenhuma visibilidade precisa mudar.

## Achado 2 (baixo) — comando de busca sugerido no plano não encontra o tipo

Task 11 diz: "`WhatsAppEventIntegrationRequest` (procurar com `grep -rn "record
WhatsAppEventIntegrationRequest" api-core/src`)". Esse grep devolve vazio — o tipo é uma `sealed
class`, não um `record`:

`api-core/src/Diax.Application/Integrations/Dtos/WhatsAppEventIntegrationRequest.cs:8`
```csharp
public sealed class WhatsAppEventIntegrationRequest
{
    public Guid? CustomerId { get; init; }
    public string? Phone { get; init; }
    public string? Event { get; init; }
    public string? Text { get; init; }
    public string? Provider { get; init; }
    public string? MessageId { get; init; }
}
```

**Correção:** buscar com `grep -rn "class WhatsAppEventIntegrationRequest" api-core/src` (ou só
`WhatsAppEventIntegrationRequest` sem o filtro "record"). Adicionar os dois campos novos do plano
(`Guid? PackageId { get; init; }`, `string? Summary { get; init; }`) funciona normalmente nesse
`class` com propriedades `init`-only — nenhuma mudança de abordagem necessária, só o comando de
busca estava errado.

## Sem achados adicionais

`IProposalRepository.GetByCustomerAsync(Guid, CancellationToken)` já existe exatamente como a Task
11 assume (`api-core/src/Diax.Domain/Customers/IProposalRepository.cs`). O switch de eventos em
`IntegrationsController.cs` (`normalizedEvent is not (...)` na linha 325, `case "sent"/"reply"/
"optout"/"failed"` nas linhas 343-388) confirma que acrescentar `"proposal_draft"` à lista negativa
e um novo `case` segue o padrão existente sem necessidade de refatoração maior.
