# Fechar venda — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Lead que respondeu vira proposta de pacote com preço fixo (Rascunho → revisão do dono → envio), com follow-up automático, e o bot do WhatsApp passa a conduzir a conversa até esse rascunho (primeiro em modo sombra).

**Architecture:** Reaproveita `Proposal` + pipeline (`Customer` é a oportunidade). Novo catálogo `service_packages`; `proposals` ganha `package_id`, `origin`, `last_follow_up_stage`. Follow-up por worker diário idempotente (padrão `CommercialBriefingWorker` + `DailyJobLedger`). Bot: script Python em `n8n-local` que roda em modo sombra a partir do `outreach-watch` e só grava decisões; nada é enviado.

**Tech Stack:** .NET (api-core: EF Core 9, SQL Server, xUnit + Moq), Next.js (crm-web, Vitest), Python 3.14 (n8n-local, WAHA local).

Design: `docs/plans/2026-10-23-fechar-venda-design.md`.

---

## Regras para quem executar

- Trabalhar no worktree `D:\claude-code\_worktrees\diax-fechar-venda`, branch de feature por grupo de PR (abaixo). **Nunca** commit/push na `main`.
- **Merge na `main` = deploy E migration em produção** (`Program.cs:353` chama `db.Database.Migrate()` no startup). Só com ok explícito do usuário.
- Testes .NET: `dotnet test api-core/tests/Diax.Tests/Diax.Tests.csproj --filter "<filtro>"`.
- Testes web: `cd crm-web && npx vitest run <arquivo>`; build: `npm run build`.
- `TaskItem.Description` é `nvarchar(256)` — sempre truncar.
- Python: `PYTHONIOENCODING=utf-8 python -u`.

Grupos de PR (CI 6/6 cada):
- **PR-a** Tasks 1–4 (pacotes: domínio, banco, API).
- **PR-b** Tasks 5–7 (proposta a partir de pacote + telas).
- **PR-c** Tasks 8–9 (follow-up de proposta).
- **PR-d** Task 10 (valor em proposta aberta no painel).
- **PR-e** Task 11 (evento `proposal_draft` + catálogo para o bot).
- Fora do repo: Tasks 12–13 (bot sombra em `n8n-local`, sem git).

---

### Task 1: Entidade `ServicePackage`

**Files:**
- Create: `api-core/src/Diax.Domain/Customers/ServicePackage.cs`
- Create: `api-core/src/Diax.Domain/Customers/IServicePackageRepository.cs`
- Test: `api-core/tests/Diax.Tests/Domain/ServicePackageTests.cs`

**Step 1: Write the failing test**

```csharp
using Diax.Domain.Customers;

namespace Diax.Tests.Domain;

public class ServicePackageTests
{
    [Fact]
    public void Create_TrimsAndKeepsFields()
    {
        var p = new ServicePackage(Guid.NewGuid(), "  Site Essencial ", "Site de 1 página", "Página única\nWhatsApp", 1800m, 15, 1);
        Assert.Equal("Site Essencial", p.Name);
        Assert.Equal(1800m, p.Price);
        Assert.Equal(15, p.DeliveryDays);
        Assert.True(p.IsActive);
    }

    [Theory]
    [InlineData("", 100)]
    [InlineData("Nome", 0)]
    [InlineData("Nome", -1)]
    public void Create_RejectsInvalid(string name, decimal price)
    {
        Assert.Throws<ArgumentException>(() => new ServicePackage(Guid.NewGuid(), name, "", "", price, 10, 0));
    }

    [Fact]
    public void ToProposalDescription_ListsScopeAndDeadline()
    {
        var p = new ServicePackage(Guid.NewGuid(), "Site", "Site institucional", "Até 5 páginas\nSEO básico", 2500m, 15, 0);
        var d = p.ToProposalDescription();
        Assert.Contains("Site institucional", d);
        Assert.Contains("• Até 5 páginas", d);
        Assert.Contains("• SEO básico", d);
        Assert.Contains("15 dias", d);
    }
}
```

**Step 2: Run to verify it fails**

Run: `dotnet test api-core/tests/Diax.Tests/Diax.Tests.csproj --filter "FullyQualifiedName~ServicePackageTests"`
Expected: build error `ServicePackage` not found.

**Step 3: Minimal implementation**

```csharp
// ServicePackage.cs
using Diax.Domain.Common;

namespace Diax.Domain.Customers;

/// <summary>Pacote de serviço com preço fixo — base das propostas (catálogo do dono).</summary>
public class ServicePackage : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string ShortDescription { get; private set; } = string.Empty;
    /// <summary>Itens do escopo, um por linha.</summary>
    public string Scope { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int DeliveryDays { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    protected ServicePackage() { } // EF Core

    public ServicePackage(Guid userId, string name, string shortDescription, string scope,
        decimal price, int deliveryDays, int sortOrder)
    {
        UserId = userId;
        Update(name, shortDescription, scope, price, deliveryDays, sortOrder, true);
    }

    public void Update(string name, string shortDescription, string scope,
        decimal price, int deliveryDays, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome do pacote é obrigatório.");
        if (price <= 0) throw new ArgumentException("Preço do pacote deve ser maior que zero.");
        if (deliveryDays < 0) throw new ArgumentException("Prazo inválido.");
        Name = name.Trim();
        ShortDescription = shortDescription?.Trim() ?? string.Empty;
        Scope = scope?.Trim() ?? string.Empty;
        Price = price;
        DeliveryDays = deliveryDays;
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public string ToProposalDescription()
    {
        var itens = Scope.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Select(i => "• " + i);
        var prazo = DeliveryDays > 0 ? $"\n\nPrazo de entrega: {DeliveryDays} dias úteis após aprovação." : "";
        return $"{ShortDescription}\n\n{string.Join('\n', itens)}{prazo}".Trim();
    }
}
```

```csharp
// IServicePackageRepository.cs
using Diax.Domain.Common;

namespace Diax.Domain.Customers;

public interface IServicePackageRepository : IRepository<ServicePackage>
{
    Task<List<ServicePackage>> GetByUserAsync(Guid userId, bool onlyActive, CancellationToken ct = default);
}
```

**Step 4: Run to verify it passes** — same command. Expected: PASS (5 tests).

**Step 5: Commit**

```bash
git add api-core/src/Diax.Domain/Customers/ServicePackage.cs api-core/src/Diax.Domain/Customers/IServicePackageRepository.cs api-core/tests/Diax.Tests/Domain/ServicePackageTests.cs
git commit -m "feat(packages): entidade ServicePackage"
```

---

### Task 2: Campos novos em `Proposal`

**Files:**
- Modify: `api-core/src/Diax.Domain/Customers/Proposal.cs`
- Test: `api-core/tests/Diax.Tests/Application/Customers/ProposalTests.cs` (acrescentar classe)

**Step 1: Write the failing test** (acrescentar no fim do arquivo)

```csharp
public class ProposalPackageAndFollowUpTests
{
    private static Proposal New() => new(Guid.NewGuid(), Guid.NewGuid(), "Site", "desc", 1800m, null, DateTime.UtcNow.AddDays(7));

    [Fact]
    public void LinkPackage_SetsPackageAndOrigin()
    {
        var p = New();
        var pkg = Guid.NewGuid();
        p.LinkPackage(pkg, ProposalOrigin.Bot);
        Assert.Equal(pkg, p.PackageId);
        Assert.Equal(ProposalOrigin.Bot, p.Origin);
    }

    [Fact]
    public void DefaultOrigin_IsManual_AndStageZero()
    {
        var p = New();
        Assert.Equal(ProposalOrigin.Manual, p.Origin);
        Assert.Equal(0, p.LastFollowUpStage);
    }

    [Fact]
    public void RegisterFollowUpStage_OnlyMovesForward()
    {
        var p = New();
        Assert.True(p.RegisterFollowUpStage(2));
        Assert.False(p.RegisterFollowUpStage(1));
        Assert.False(p.RegisterFollowUpStage(2));
        Assert.Equal(2, p.LastFollowUpStage);
    }

    [Fact]
    public void IsOpen_TrueForDraftSentAccepted()
    {
        var p = New();
        Assert.True(p.IsOpen);
        p.MarkSent(); Assert.True(p.IsOpen);
        p.Cancel(); Assert.False(p.IsOpen);
    }
}
```

**Step 2:** Run `--filter "FullyQualifiedName~ProposalPackageAndFollowUpTests"`. Expected: build error.

**Step 3: Implementation** — em `Proposal.cs`, depois do enum `ProposalStatus`:

```csharp
public enum ProposalOrigin
{
    Manual = 0,
    Bot = 1,
}
```

Dentro da classe, depois de `CoverImageUrl`:

```csharp
    /// <summary>Pacote do catálogo que originou a proposta (opcional).</summary>
    public Guid? PackageId { get; private set; }

    public ProposalOrigin Origin { get; private set; } = ProposalOrigin.Manual;

    /// <summary>Maior etapa de follow-up já gerada (0 = nenhuma). Ver ProposalFollowUpService.</summary>
    public int LastFollowUpStage { get; private set; }

    public bool IsOpen => Status is ProposalStatus.Draft or ProposalStatus.Sent or ProposalStatus.Accepted;

    public void LinkPackage(Guid packageId, ProposalOrigin origin)
    {
        PackageId = packageId;
        Origin = origin;
    }

    public void SetOrigin(ProposalOrigin origin) => Origin = origin;

    /// <summary>Retorna true se a etapa é nova (avança); false se já foi gerada.</summary>
    public bool RegisterFollowUpStage(int stage)
    {
        if (stage <= LastFollowUpStage) return false;
        LastFollowUpStage = stage;
        return true;
    }
```

**Step 4:** Run the filter. Expected: PASS. Run also `--filter "FullyQualifiedName~Proposal"` (nada quebrou).

**Step 5: Commit** — `git commit -am "feat(proposals): package_id, origin e etapa de follow-up"`

---

### Task 3: Banco — configuração EF, repositório e migration

**Files:**
- Create: `api-core/src/Diax.Infrastructure/Data/Configurations/ServicePackageConfiguration.cs`
- Modify: `api-core/src/Diax.Infrastructure/Data/Configurations/ProposalConfiguration.cs`
- Modify: `api-core/src/Diax.Infrastructure/Data/DiaxDbContext.cs:75` (DbSet)
- Create: `api-core/src/Diax.Infrastructure/Data/Repositories/ServicePackageRepository.cs`
- Modify: `api-core/src/Diax.Infrastructure/DependencyInjection.cs:274` (registro)
- Create (gerado): `api-core/src/Diax.Infrastructure/Data/Migrations/<ts>_AddServicePackages.cs`

**Step 1: Configuration**

```csharp
using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diax.Infrastructure.Data.Configurations;

public class ServicePackageConfiguration : IEntityTypeConfiguration<ServicePackage>
{
    public void Configure(EntityTypeBuilder<ServicePackage> builder)
    {
        builder.ToTable("service_packages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(120);
        builder.Property(x => x.ShortDescription).HasColumnName("short_description").HasMaxLength(500);
        builder.Property(x => x.Scope).HasColumnName("scope").HasMaxLength(4000);
        builder.Property(x => x.Price).HasColumnName("price").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.DeliveryDays).HasColumnName("delivery_days").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
        builder.HasIndex(x => new { x.UserId, x.IsActive, x.SortOrder });
    }
}
```

Em `ProposalConfiguration.cs`, antes de `builder.HasIndex(x => x.PublicToken)`:

```csharp
        builder.Property(x => x.PackageId).HasColumnName("package_id");
        builder.Property(x => x.Origin).HasColumnName("origin").IsRequired().HasDefaultValue(ProposalOrigin.Manual);
        builder.Property(x => x.LastFollowUpStage).HasColumnName("last_follow_up_stage").IsRequired().HasDefaultValue(0);
        builder.HasIndex(x => new { x.Status, x.SentAt });
```

Em `DiaxDbContext.cs`, logo abaixo da linha 75:

```csharp
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
```

**Step 2: Repository**

```csharp
using Diax.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ServicePackageRepository : Repository<ServicePackage>, IServicePackageRepository
{
    public ServicePackageRepository(DiaxDbContext context) : base(context) { }

    public async Task<List<ServicePackage>> GetByUserAsync(Guid userId, bool onlyActive, CancellationToken ct = default)
        => await DbSet.Where(p => p.UserId == userId && (!onlyActive || p.IsActive))
                      .OrderBy(p => p.SortOrder).ThenBy(p => p.Price)
                      .ToListAsync(ct);
}
```

Em `DependencyInjection.cs`, ao lado da linha 274:

```csharp
        services.AddScoped<IServicePackageRepository, ServicePackageRepository>();
```

**Step 3: Generate migration**

Run (PowerShell, de `api-core`): `.\scriptsdd-migration.ps1 -Name "AddServicePackages"` (padrão do `CLAUDE.md`; só gera código). **Não** rodar `update-db.ps1`: a migration vai pra produção sozinha no startup depois do merge.
Expected: arquivo novo com `CreateTable("service_packages")` e 3 `AddColumn` em `proposals` (`package_id` nullable, `origin` default 0, `last_follow_up_stage` default 0) + índice. **Ler o arquivo**: não pode ter `DropTable`, `DropColumn` nem `AlterColumn` de outras tabelas. Se tiver, o snapshot está defasado — parar e investigar antes de seguir.

**Step 4: Build + testes**

Run: `dotnet build api-core/Diax.CRM.sln -c Release` → 0 erros. Run: `dotnet test api-core/tests/Diax.Tests/Diax.Tests.csproj` (suite inteira) → PASS.

**Step 5: Commit** — `git add` dos arquivos acima + migration + snapshot; `git commit -m "feat(packages): tabela service_packages e colunas novas em proposals"`

---

### Task 4: Serviço e API de pacotes

**Files:**
- Create: `api-core/src/Diax.Application/Customers/ServicePackageService.cs`
- Modify: `api-core/src/Diax.Application/DependencyInjection.cs:64` (`services.AddScoped<ServicePackageService>();`)
- Create: `api-core/src/Diax.Api/Controllers/V1/ServicePackagesController.cs`
- Test: `api-core/tests/Diax.Tests/Application/Customers/ServicePackageServiceTests.cs`

**Step 1: Write the failing test**

```csharp
using Diax.Application.Customers;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Moq;

namespace Diax.Tests.Application.Customers;

public class ServicePackageServiceTests
{
    private readonly Mock<IServicePackageRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private ServicePackageService Svc() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Create_Valid_SavesAndReturnsDto()
    {
        var user = Guid.NewGuid();
        var r = await Svc().CreateAsync(new UpsertServicePackageRequest("Site", "desc", "a\nb", 1800m, 15, 1, true), user);
        Assert.True(r.IsSuccess);
        Assert.Equal("Site", r.Value.Name);
        _repo.Verify(x => x.AddAsync(It.Is<ServicePackage>(p => p.UserId == user), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_Invalid_ReturnsValidationFailure()
    {
        var r = await Svc().CreateAsync(new UpsertServicePackageRequest("", "", "", 0m, 0, 0, true), Guid.NewGuid());
        Assert.False(r.IsSuccess);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_OtherUsersPackage_NotFound()
    {
        var pkg = new ServicePackage(Guid.NewGuid(), "Site", "", "", 100m, 1, 0);
        _repo.Setup(x => x.GetByIdAsync(pkg.Id, It.IsAny<CancellationToken>())).ReturnsAsync(pkg);
        var r = await Svc().UpdateAsync(pkg.Id, new UpsertServicePackageRequest("X", "", "", 1m, 1, 0, true), Guid.NewGuid());
        Assert.False(r.IsSuccess);
    }
}
```

> Se `IRepository<T>.GetByIdAsync`/`AddAsync`/`IUnitOfWork.SaveChangesAsync` tiverem assinatura diferente, ajustar o `Setup`/`Verify` conforme `api-core/src/Diax.Domain/Common/IRepository.cs` — não mudar a interface.

**Step 2:** `--filter "FullyQualifiedName~ServicePackageServiceTests"` → FAIL (build).

**Step 3: Implementation**

```csharp
using Diax.Application.Common;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Shared.Results;

namespace Diax.Application.Customers;

public record UpsertServicePackageRequest(
    string Name, string ShortDescription, string Scope, decimal Price, int DeliveryDays, int SortOrder, bool IsActive);

public record ServicePackageDto(
    Guid Id, string Name, string ShortDescription, string Scope, decimal Price, int DeliveryDays, int SortOrder, bool IsActive);

public class ServicePackageService : IApplicationService
{
    private readonly IServicePackageRepository _repo;
    private readonly IUnitOfWork _uow;

    public ServicePackageService(IServicePackageRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public static ServicePackageDto ToDto(ServicePackage p) =>
        new(p.Id, p.Name, p.ShortDescription, p.Scope, p.Price, p.DeliveryDays, p.SortOrder, p.IsActive);

    public async Task<List<ServicePackageDto>> ListAsync(Guid userId, bool onlyActive, CancellationToken ct = default)
        => (await _repo.GetByUserAsync(userId, onlyActive, ct)).Select(ToDto).ToList();

    public async Task<Result<ServicePackageDto>> CreateAsync(UpsertServicePackageRequest r, Guid userId, CancellationToken ct = default)
    {
        ServicePackage p;
        try { p = new ServicePackage(userId, r.Name, r.ShortDescription, r.Scope, r.Price, r.DeliveryDays, r.SortOrder); }
        catch (ArgumentException ex) { return Result.Failure<ServicePackageDto>(Error.Validation("ServicePackage", ex.Message)); }
        if (!r.IsActive) p.Update(r.Name, r.ShortDescription, r.Scope, r.Price, r.DeliveryDays, r.SortOrder, false);
        await _repo.AddAsync(p, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(ToDto(p));
    }

    public async Task<Result<ServicePackageDto>> UpdateAsync(Guid id, UpsertServicePackageRequest r, Guid userId, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct);
        if (p == null || p.UserId != userId)
            return Result.Failure<ServicePackageDto>(Error.NotFound("ServicePackage", id.ToString()));
        try { p.Update(r.Name, r.ShortDescription, r.Scope, r.Price, r.DeliveryDays, r.SortOrder, r.IsActive); }
        catch (ArgumentException ex) { return Result.Failure<ServicePackageDto>(Error.Validation("ServicePackage", ex.Message)); }
        await _repo.UpdateAsync(p, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(ToDto(p));
    }
}
```

Controller (mesmo padrão de `ProposalsController.cs:12-40`):

```csharp
using Asp.Versioning;
using Diax.Application.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/service-packages")]
[Produces("application/json")]
public class ServicePackagesController : BaseApiController
{
    private readonly ServicePackageService _service;
    public ServicePackagesController(ServicePackageService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool onlyActive = false, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return Ok(await _service.ListAsync(userId.Value, onlyActive, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertServicePackageRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return HandleResult(await _service.CreateAsync(request, userId.Value, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertServicePackageRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "Usuário não autenticado." });
        return HandleResult(await _service.UpdateAsync(id, request, userId.Value, ct));
    }
}
```

**Step 4:** filtro dos testes → PASS; `dotnet build api-core/Diax.CRM.sln -c Release` → 0 erros.

**Step 5: Commit** — `git commit -m "feat(packages): CRUD de pacotes (service + /service-packages)"`. **Abrir PR-a** (branch `feat/service-packages`), aguardar CI 6/6. **Não mergear sem ok.**

---

### Task 5: Proposta a partir de pacote (API)

**Files:**
- Modify: `api-core/src/Diax.Application/Customers/ProposalService.cs:13-19` (request), `:21-35` (DTO), `:106-145` (CreateAsync), construtor (+ `IServicePackageRepository`)
- Test: `api-core/tests/Diax.Tests/Application/Customers/ProposalFromPackageTests.cs`

**Step 1: Write the failing test** — montar `ProposalService` com Moq para todas as dependências do construtor (`ProposalService.cs:73-88`) mais `IServicePackageRepository`. Casos:

```csharp
// 1) PackageId informado e Title/Description/Amount vazios/0 → proposta usa Name, ToProposalDescription(), Price e PackageId.
// 2) PackageId informado e Amount > 0 → Amount do request prevalece (ajuste manual do dono).
// 3) PackageId de outro usuário ou inexistente → Result.Failure (NotFound) e nada salvo.
// 4) Sem PackageId → comportamento atual (Origin = Manual, PackageId null).
```

Escrever os 4 `[Fact]` com asserts sobre o `Proposal` capturado em `_proposalRepo.Setup(x => x.AddAsync(It.IsAny<Proposal>(), ...)).Callback<Proposal, CancellationToken>((p, _) => captured = p)`.

**Step 2:** `--filter "FullyQualifiedName~ProposalFromPackageTests"` → FAIL.

**Step 3: Implementation**

```csharp
public record CreateProposalRequest(
    Guid CustomerId,
    string Title,
    string Description,
    decimal Amount,
    string? PixKey,
    DateTime? ValidUntil,
    Guid? PackageId = null);
```

`ProposalDto`: acrescentar no fim `Guid? PackageId = null, ProposalOrigin Origin = ProposalOrigin.Manual` e preencher no mapper existente.

No início de `CreateAsync`, depois de carregar o customer:

```csharp
        ServicePackage? package = null;
        if (request.PackageId is { } packageId)
        {
            package = await _servicePackageRepository.GetByIdAsync(packageId, ct);
            if (package == null || package.UserId != userId)
                return Result.Failure<ProposalDto>(Error.NotFound("ServicePackage", packageId.ToString()));
        }

        var title = string.IsNullOrWhiteSpace(request.Title) && package != null ? package.Name : request.Title;
        var description = string.IsNullOrWhiteSpace(request.Description) && package != null ? package.ToProposalDescription() : request.Description;
        var amount = request.Amount <= 0 && package != null ? package.Price : request.Amount;
```

E passar `title`, `description`, `amount` para o construtor de `Proposal`; logo depois do `new Proposal(...)`: `if (package != null) proposal.LinkPackage(package.Id, ProposalOrigin.Manual);`. Usar `amount` também em `UpdateDealInfo`.

**Step 4:** filtro → PASS; `--filter "FullyQualifiedName~Proposal"` → PASS.

**Step 5: Commit** — `git commit -m "feat(proposals): gerar proposta a partir de pacote"`

---

### Task 6: Tela de pacotes (crm-web)

**Files:**
- Create: `crm-web/src/services/servicePackages.ts`
- Create: `crm-web/src/lib/packageDraft.ts`
- Test: `crm-web/src/lib/__tests__/packageDraft.test.ts`
- Create: `crm-web/src/app/propostas/pacotes/page.tsx`
- Modify: link "Pacotes" no cabeçalho de `crm-web/src/app/propostas/page.tsx`

**Step 1: Write the failing test**

```ts
import { describe, expect, it } from 'vitest';
import { packageToDraft, parseBRL } from '../packageDraft';

describe('packageToDraft', () => {
  it('monta título, descrição com itens e valor', () => {
    const d = packageToDraft({ id: '1', name: 'Site Essencial', shortDescription: 'Site de 1 página',
      scope: 'Página única\nBotão WhatsApp', price: 1800, deliveryDays: 15, sortOrder: 0, isActive: true });
    expect(d.title).toBe('Site Essencial');
    expect(d.description).toContain('• Página única');
    expect(d.description).toContain('15 dias úteis');
    expect(d.amount).toBe('1800');
  });
});

describe('parseBRL', () => {
  it('aceita 2.500,00 e 2500', () => {
    expect(parseBRL('2.500,00')).toBe(2500);
    expect(parseBRL('2500')).toBe(2500);
    expect(Number.isNaN(parseBRL('abc'))).toBe(true);
  });
});
```

**Step 2:** `cd crm-web && npx vitest run src/lib/__tests__/packageDraft.test.ts` → FAIL.

**Step 3: Implementation**

```ts
// src/services/servicePackages.ts
import { apiFetch } from './api';

export interface ServicePackage {
  id: string; name: string; shortDescription: string; scope: string;
  price: number; deliveryDays: number; sortOrder: number; isActive: boolean;
}
export type UpsertServicePackage = Omit<ServicePackage, 'id'>;

export const listServicePackages = (onlyActive = false) =>
  apiFetch<ServicePackage[]>(`/service-packages?onlyActive=${onlyActive}`, { method: 'GET' });
export const createServicePackage = (data: UpsertServicePackage) =>
  apiFetch<ServicePackage>('/service-packages', { method: 'POST', body: JSON.stringify(data) });
export const updateServicePackage = (id: string, data: UpsertServicePackage) =>
  apiFetch<ServicePackage>(`/service-packages/${id}`, { method: 'PUT', body: JSON.stringify(data) });
```

```ts
// src/lib/packageDraft.ts
import type { ServicePackage } from '@/services/servicePackages';

// Mesmo texto de ServicePackage.ToProposalDescription() no backend.
export function packageToDraft(p: ServicePackage) {
  const itens = p.scope.split('\n').map(s => s.trim()).filter(Boolean).map(s => `• ${s}`);
  const prazo = p.deliveryDays > 0 ? `\n\nPrazo de entrega: ${p.deliveryDays} dias úteis após aprovação.` : '';
  return {
    title: p.name,
    description: `${p.shortDescription}\n\n${itens.join('\n')}${prazo}`.trim(),
    amount: String(p.price),
  };
}

export function parseBRL(v: string): number {
  const n = Number(v.trim().replace(/\./g, '').replace(',', '.'));
  return n;
}
```

`propostas/pacotes/page.tsx`: lista (nome, preço `BRL.format`, prazo, ativo) + formulário único criar/editar (campos do `UpsertServicePackage`, `scope` em `<textarea>` "um item por linha"), mesmo visual de `propostas/page.tsx` (copiar classes de card/botão de lá). Estados de carregando/erro iguais aos de `propostas/page.tsx:49-56`.

**Step 4:** vitest → PASS; `npm run build` → sucesso.

**Step 5: Commit** — `git commit -m "feat(crm-web): tela de pacotes de serviço"`

---

### Task 7: Seletor de pacote no modal "Gerar proposta" do pipeline

**Files:**
- Modify: `crm-web/src/services/proposals.ts` (`CreateProposalRequest.packageId?: string | null`; `Proposal.packageId?`, `origin?`)
- Modify: `crm-web/src/app/pipeline/page.tsx:93-160` (estado + `openProposal` + `submitProposal`) e `:415-435` (JSX do modal)

**Step 1:** Estado novo: `const [packages, setPackages] = useState<ServicePackage[]>([]); const [pPackageId, setPPackageId] = useState<string>('');` — carregar com `listServicePackages(true)` ao abrir o modal (só uma vez, se `packages.length === 0`).

**Step 2:** `<select>` acima do título: opção "Sem pacote (texto livre)" + um `<option>` por pacote (`{name} — {BRL.format(price)}`). `onChange`: se escolher pacote, `const d = packageToDraft(pkg); setPTitle(d.title); setPDesc(d.description); setPAmount(d.amount); setPPackageId(pkg.id)`. O dono continua podendo editar os campos.

**Step 3:** `submitProposal`: trocar o parse manual por `parseBRL(pAmount)`; validade **7 dias** (era 15, decisão do design) e enviar `packageId: pPackageId || null`.

**Step 4:** `npm run build` → sucesso. Teste manual local (`npm run dev` + API local ou prod em leitura): abrir pipeline → card → Gerar proposta → escolher pacote → campos preenchidos. **Não criar proposta real em produção para teste.**

**Step 5: Commit** — `git commit -m "feat(pipeline): gerar proposta escolhendo pacote"`. **Abrir PR-b**, CI 6/6, aguardar ok.

---

### Task 8: Regra de follow-up de proposta (pura, testável)

**Files:**
- Create: `api-core/src/Diax.Application/Customers/ProposalFollowUpService.cs`
- Modify: `api-core/src/Diax.Domain/Customers/IProposalRepository.cs` + `Diax.Infrastructure/Data/Repositories/ProposalRepository.cs` (`GetOpenSentAsync`)
- Modify: `api-core/src/Diax.Application/DependencyInjection.cs` (registro)
- Test: `api-core/tests/Diax.Tests/Application/Customers/ProposalFollowUpTests.cs`

**Etapas:** `1 = NaoVista` (Enviada há ≥48h, `ViewCount == 0`), `2 = VistaSemAceite` (Enviada há ≥48h, `ViewCount > 0`, status `Sent`), `3 = Vencida` (`IsExpired` e status `Sent`). Só gera tarefa se `RegisterFollowUpStage(etapa)` devolver true.

**Step 1: Write the failing test**

```csharp
using Diax.Application.Customers;
using Diax.Domain.Customers;

namespace Diax.Tests.Application.Customers;

public class ProposalFollowUpTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact] public void Draft_NoStage() =>
        Assert.Equal(0, ProposalFollowUpService.StageFor(ProposalStatus.Draft, Now.AddDays(-5), 0, false, Now));

    [Fact] public void SentRecent_NoStage() =>
        Assert.Equal(0, ProposalFollowUpService.StageFor(ProposalStatus.Sent, Now.AddHours(-47), 0, false, Now));

    [Fact] public void Sent48hNotViewed_Stage1() =>
        Assert.Equal(1, ProposalFollowUpService.StageFor(ProposalStatus.Sent, Now.AddHours(-49), 0, false, Now));

    [Fact] public void Sent48hViewed_Stage2() =>
        Assert.Equal(2, ProposalFollowUpService.StageFor(ProposalStatus.Sent, Now.AddHours(-49), 3, false, Now));

    [Fact] public void Expired_Stage3() =>
        Assert.Equal(3, ProposalFollowUpService.StageFor(ProposalStatus.Sent, Now.AddDays(-9), 1, true, Now));

    [Fact] public void Accepted_NoStage() =>
        Assert.Equal(0, ProposalFollowUpService.StageFor(ProposalStatus.Accepted, Now.AddDays(-9), 1, true, Now));

    [Theory]
    [InlineData(1, "Proposta não aberta")]
    [InlineData(2, "Proposta vista sem aceite")]
    [InlineData(3, "Proposta vencida")]
    public void TaskTitle_PerStage(int stage, string prefix) =>
        Assert.StartsWith(prefix, ProposalFollowUpService.TaskTitle(stage, "Padaria X"));
}
```

**Step 2:** `--filter "FullyQualifiedName~ProposalFollowUpTests"` → FAIL.

**Step 3: Implementation**

```csharp
using Diax.Application.Common;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Tasks;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers;

/// <summary>Cria 1 tarefa por proposta por etapa (não vista / vista sem aceite / vencida). Idempotente via Proposal.LastFollowUpStage.</summary>
public class ProposalFollowUpService : IApplicationService
{
    public static readonly TimeSpan Wait = TimeSpan.FromHours(48);
    private readonly IProposalRepository _proposals;
    private readonly ICustomerRepository _customers;
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProposalFollowUpService> _logger;

    public ProposalFollowUpService(IProposalRepository proposals, ICustomerRepository customers, ITaskRepository tasks,
        IUnitOfWork uow, ILogger<ProposalFollowUpService> logger)
    { _proposals = proposals; _customers = customers; _tasks = tasks; _uow = uow; _logger = logger; }

    public static int StageFor(ProposalStatus status, DateTime? sentAt, int viewCount, bool isExpired, DateTime now)
    {
        if (status != ProposalStatus.Sent || sentAt == null) return 0;
        if (isExpired) return 3;
        if (now - sentAt.Value < Wait) return 0;
        return viewCount == 0 ? 1 : 2;
    }

    public static string TaskTitle(int stage, string customerName) => stage switch
    {
        1 => $"Proposta não aberta: {customerName}",
        2 => $"Proposta vista sem aceite: {customerName}",
        3 => $"Proposta vencida: {customerName}",
        _ => $"Proposta: {customerName}",
    };

    private static string TaskHint(int stage) => stage switch
    {
        1 => "Reenviar o link ou chamar no WhatsApp.",
        2 => "Cliente abriu a proposta. Fazer follow-up e tirar dúvidas.",
        3 => "Renovar a proposta (nova validade) ou encerrar o negócio.",
        _ => "",
    };

    public async Task<int> RunAsync(Guid ownerUserId, DateTime now, CancellationToken ct = default)
    {
        var created = 0;
        foreach (var p in await _proposals.GetOpenSentAsync(ownerUserId, ct))
        {
            var stage = StageFor(p.Status, p.SentAt, p.ViewCount, p.IsExpired, now);
            if (stage == 0 || !p.RegisterFollowUpStage(stage)) continue;
            var customer = await _customers.GetByIdAsync(p.CustomerId, ct);
            var name = customer?.Name ?? p.Title;
            var desc = $"{p.Title} · R$ {p.Amount:N2} · {p.ViewCount} visualização(ões). {TaskHint(stage)}";
            await _tasks.AddAsync(new TaskItem
            {
                Title = TaskTitle(stage, name),
                Description = desc.Length > 256 ? desc[..256] : desc,
                Priority = stage == 2 ? TaskItemPriority.Urgent : TaskItemPriority.High,
                DueDate = now.Date.AddDays(1),
                UserId = ownerUserId,
                CustomerId = p.CustomerId,
            }, ct);
            await _proposals.UpdateAsync(p, ct);
            created++;
        }
        if (created > 0) await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("[ProposalFollowUp] {Created} tarefa(s) criada(s)", created);
        return created;
    }
}
```

Repositório:

```csharp
    // IProposalRepository
    Task<List<Proposal>> GetOpenSentAsync(Guid userId, CancellationToken cancellationToken = default);

    // ProposalRepository
    public async Task<List<Proposal>> GetOpenSentAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.Where(p => p.UserId == userId && p.Status == ProposalStatus.Sent)
                      .OrderBy(p => p.SentAt).Take(200).ToListAsync(cancellationToken);
```

Registro: `services.AddScoped<ProposalFollowUpService>();` ao lado de `ProposalService`.

**Step 4:** filtro → PASS. Acrescentar 1 teste de `RunAsync` com Moq: mesma proposta processada 2× no mesmo estágio → `ITaskRepository.AddAsync` chamado **1** vez.

**Step 5: Commit** — `git commit -m "feat(proposals): regra de follow-up por etapa, idempotente"`

---

### Task 9: Worker diário de follow-up

**Files:**
- Create: `api-core/src/Diax.Infrastructure/Workers/ProposalFollowUpWorker.cs`
- Modify: `api-core/src/Diax.Infrastructure/DependencyInjection.cs:559` — logo abaixo de `services.AddHostedService<Diax.Infrastructure.Workers.CommercialBriefingWorker>();` acrescentar `services.AddHostedService<Diax.Infrastructure.Workers.ProposalFollowUpWorker>();`.

**Step 1:** Copiar `CommercialBriefingWorker.cs` inteiro (padrão poll 10 min + `DailyJobLedger` + `ResolveOwnerAsync` + `TryAlertFailureAsync`) e trocar:
- `JobName = "proposal-followup"`, `RunAtUtcHour = 12`, `RunAtUtcMinute = 0` (09h BRT);
- a chamada do serviço por `scope.ServiceProvider.GetRequiredService<ProposalFollowUpService>().RunAsync(ownerId.Value, DateTime.UtcNow, stoppingToken)`;
- prefixo de log `[ProposalFollowUp]`. Rodar também no fim de semana não é preciso: manter o `continue` de sábado/domingo.

**Step 2:** `dotnet build api-core/Diax.CRM.sln -c Release` → 0 erros; suite inteira → PASS.

**Step 3: Commit** — `git commit -m "feat(workers): follow-up diário de propostas (job_runs)"`. **Abrir PR-c**, CI, aguardar ok.

---

### Task 10: Valor em proposta aberta no painel

**Files:**
- Modify: `api-core/src/Diax.Application/Customers/SalesDashboardService.cs:14-26` (DTO) e o método que calcula `ProposalAcceptanceRate` (`:141`)
- Modify: `crm-web/src/services/salesDashboard.ts` + `crm-web/src/app/dashboard-comercial/page.tsx` (card)
- Test: `api-core/tests/Diax.Tests/Application/Customers/SalesDashboardTests.cs`

**Step 1: Failing test**

```csharp
    [Fact]
    public void OpenValue_SumsSentAndAccepted()
    {
        var summary = new List<(ProposalStatus Status, int Count, decimal Total)>
        {
            (ProposalStatus.Draft, 1, 900m), (ProposalStatus.Sent, 2, 5000m),
            (ProposalStatus.Accepted, 1, 1800m), (ProposalStatus.Paid, 1, 2500m),
        };
        Assert.Equal(6800m, SalesDashboardService.OpenValue(summary));
    }
```

**Step 2:** filtro `SalesDashboardTests` → FAIL.

**Step 3:** `public static decimal OpenValue(List<(ProposalStatus Status, int Count, decimal Total)> s) => s.Where(x => x.Status is ProposalStatus.Sent or ProposalStatus.Accepted).Sum(x => x.Total);` — adicionar `decimal OpenProposalsValue` no **fim** do `SalesDashboardDto` e preencher. Front: tipo + 1 card "Em proposta aberta" ao lado de `WonLast30DaysValue`, mesmo componente dos cards vizinhos.

**Step 4:** testes → PASS; `npm run build` → sucesso.

**Step 5: Commit** — `git commit -m "feat(dashboard): valor em proposta aberta"`. **Abrir PR-d**, CI, aguardar ok.

---

### Task 11: Evento `proposal_draft` e catálogo para o bot

**Files:**
- Modify: `api-core/src/Diax.Application/Customers/ProposalService.cs` (novo `CreateBotDraftAsync`)
- Modify: `api-core/src/Diax.Api/Controllers/V1/IntegrationsController.cs:295-330` (aceitar `proposal_draft`) + novo `GET integrations/service-packages`
- Modify: `WhatsAppEventIntegrationRequest` (procurar com `grep -rn "record WhatsAppEventIntegrationRequest" api-core/src`) → `Guid? PackageId`, `string? Summary`
- Test: `api-core/tests/Diax.Tests/Application/Customers/ProposalBotDraftTests.cs`

**Step 1: Failing tests** (Moq, mesmo setup da Task 5):

```csharp
// 1) Sem proposta aberta → cria Proposal Draft com Origin=Bot, PackageId, Amount=Price; cliente vai p/ Negotiating; cria TaskItem Urgent "Revisar e enviar proposta: {nome}" com Summary truncado em 256.
// 2) Já existe proposta IsOpen para o cliente → não cria outra; retorna Success com a existente (idempotente).
// 3) PackageId inexistente/inativo → Failure Validation, nada salvo.
```

**Step 2:** filtro `ProposalBotDraftTests` → FAIL.

**Step 3: Implementation**

```csharp
    public async Task<Result<ProposalDto>> CreateBotDraftAsync(
        Guid customerId, Guid packageId, string? summary, Guid ownerUserId, CancellationToken ct = default)
    {
        var open = (await _proposalRepository.GetByCustomerAsync(customerId, ct)).FirstOrDefault(p => p.IsOpen);
        if (open != null) return Result.Success(await ToDtoAsync(open, ct)); // 1 rascunho aberto por lead

        var package = await _servicePackageRepository.GetByIdAsync(packageId, ct);
        if (package == null || !package.IsActive || package.UserId != ownerUserId)
            return Result.Failure<ProposalDto>(Error.Validation("ServicePackage", "Pacote inexistente ou inativo."));

        var created = await CreateAsync(new CreateProposalRequest(
            customerId, package.Name, package.ToProposalDescription(), package.Price, null,
            DateTime.UtcNow.AddDays(7), package.Id), ownerUserId, ct);
        if (!created.IsSuccess) return created;

        var proposal = await _proposalRepository.GetByIdAsync(created.Value.Id, ct);
        proposal!.SetOrigin(ProposalOrigin.Bot);
        await _proposalRepository.UpdateAsync(proposal, ct);

        var desc = string.IsNullOrWhiteSpace(summary) ? $"Pacote {package.Name}" : summary.Trim();
        await _taskRepository.AddAsync(new TaskItem
        {
            Title = $"Revisar e enviar proposta: {created.Value.CustomerName}",
            Description = desc.Length > 256 ? desc[..256] : desc,
            Priority = TaskItemPriority.Urgent,
            DueDate = DateTime.UtcNow.Date.AddHours(21),
            UserId = ownerUserId,
            CustomerId = customerId,
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success(await ToDtoAsync(proposal, ct));
    }
```

> `ToDtoAsync`: usar o mapper que `CreateAsync` já usa para devolver `ProposalDto`; se ele for síncrono/privado com outro nome, reutilizar esse em vez de criar outro. `ITaskRepository` entra no construtor de `ProposalService`.

Controller: no `normalizedEvent is not (...)` acrescentar `"proposal_draft"`; novo `case "proposal_draft":` exige `request.PackageId` (senão 400), resolve dono com `ResolveOwnerUserIdAsync`, chama `CreateBotDraftAsync` e devolve `Ok(new { proposalId, created = true|false })`. Novo endpoint:

```csharp
    [HttpGet("service-packages")]
    public async Task<IActionResult> ServicePackagesForBot(
        [FromHeader(Name = "X-Integration-Key")] string? integrationKey, CancellationToken ct)
```

com a **mesma checagem de chave** de `WhatsAppEvent` (extrair para um método privado `CheckIntegrationKey` e reutilizar nos dois — não duplicar) e retorno `ServicePackageService.ListAsync(owner, onlyActive: true)` só com `id, name, shortDescription, price, deliveryDays`.

**Step 4:** testes → PASS; suite inteira → PASS; build → 0 erros.

**Step 5: Commit** — `git commit -m "feat(integrations): proposal_draft do bot + catálogo de pacotes"`. **Abrir PR-e**, CI, aguardar ok.

---

### Task 12: Bot vendedor em modo sombra (n8n-local, fora do git)

**Files:**
- Create: `D:\claude-code\n8n-local\bot_vendas.py`
- Create: `D:\claude-code\n8n-local\test_bot_vendas.py`
- Log: `D:\claude-code\n8n-local\logs\bot-sombra.jsonl`

**Step 1: Failing test** (regras, sem rede):

```python
import bot_vendas as b

CAT = [{'id': 'p1', 'name': 'Site Essencial', 'price': 1800}, {'id': 'p2', 'name': 'Site + Agendamento', 'price': 2800}]

def test_preco_fora_do_catalogo_e_bloqueado():
    d = b.validar({'reply': 'Fica R$ 999', 'package_id': 'p1', 'pronto_para_proposta': False}, CAT)
    assert d['bloqueado'] and 'preco' in d['motivo']

def test_preco_do_catalogo_passa():
    d = b.validar({'reply': 'A partir de R$ 1.800', 'package_id': 'p1', 'pronto_para_proposta': False}, CAT)
    assert not d['bloqueado']

def test_pacote_inexistente_nao_fica_pronto():
    d = b.validar({'reply': 'ok', 'package_id': 'zzz', 'pronto_para_proposta': True}, CAT)
    assert d['pronto_para_proposta'] is False

def test_dono_falou_nas_ultimas_24h_silencia():
    hist = [{'fromMe': True, 'source': 'manual', 'timestamp': 1000}]
    assert b.dono_assumiu(hist, agora=1000 + 3600) is True
    assert b.dono_assumiu(hist, agora=1000 + 25 * 3600) is False

def test_limite_de_6_respostas():
    hist = [{'fromMe': True, 'source': 'bot', 'timestamp': i} for i in range(6)]
    assert b.estourou_limite(hist) is True
```

**Step 2:** `cd D:\claude-code\n8n-local && python -m pytest test_bot_vendas.py -q` → FAIL (módulo não existe).

**Step 3: Implementation** — `bot_vendas.py` com:
- `validar(decisao, catalogo)`: extrai valores `R$ x` do `reply` (regex `R\$\s*([\d\.]+(?:,\d{2})?)`); qualquer valor que não seja `price` de um item do catálogo → `bloqueado=True, motivo='preco fora do catalogo'`; `package_id` que não existe → `pronto_para_proposta=False`.
- `dono_assumiu(hist, agora)`: alguma msg `fromMe` com `source != 'bot'` nas últimas 24h. (Confirmar no WAHA qual campo distingue envio pela API de envio pelo celular; se não existir, usar `outreach-sent.json`/ledger como lista do que o bot mandou e tratar o resto como manual.)
- `estourou_limite(hist)`: ≥6 msgs `fromMe` do bot.
- `decidir(chat_id, customer_id)`: busca histórico (15 últimas, `GET /api/default/chats/{id}/messages?limit=15`, mesma API/key do `outreach-watch.py`), catálogo (`GET /api/v1/integrations/service-packages` com `X-Integration-Key` = `DIAX_SEND_EMAIL_KEY` do vault), chama LLM (Groq `openai/gpt-oss-120b` → fallback Gemini `gemini-2.5-flash`, mesmas credenciais do `n8n-local/.env`) pedindo **só JSON** `{reply, intent, package_id, pronto_para_proposta, resumo}` com o system prompt do design (só preço do catálogo, sem desconto/link/prazo fora do pacote, máx 3 frases). Aplica `validar`, `dono_assumiu`, `estourou_limite`.
- `main --sombra`: para cada chat recebido em argv, grava 1 linha JSON em `logs/bot-sombra.jsonl` (`ts, chat_id, ultima_msg_lead, decisao, bloqueado, motivo`). **Nunca chama sendText nem o CRM.** Falha de LLM → grava `{erro: tipo}` e segue.

**Step 4:** `python -m pytest test_bot_vendas.py -q` → 5 passed. Rodar `python -u bot_vendas.py --sombra <chat_id de um lead que já respondeu>` e conferir a linha no `.jsonl`.

**Step 5:** backup antes de mexer em qualquer script existente (`logs/<arquivo>.bak-AAAAMMDD`), como já é padrão nesta pasta.

---

### Task 13: Ligar a sombra no `outreach-watch`

**Files:**
- Modify: `D:\claude-code\n8n-local\outreach-watch.py:150-152` (laço `for cid, corpo, ult in novos`)

**Step 1:** backup `logs/outreach-watch.py.bak-<data>`.

**Step 2:** dentro do laço, **antes** do `reinjetar_webhook`, chamar `subprocess.run([sys.executable, '-u', 'bot_vendas.py', '--sombra', cid], timeout=90)` dentro de `try/except` que só loga — a sombra nunca pode atrasar nem quebrar o fluxo atual.

**Step 3:** rodar `python -u outreach-watch.py --quieto` uma vez; conferir `logs/bot-sombra.jsonl` e que o comportamento atual (reinjeção no bot atual) continua igual no `outreach-audit.log`.

**Step 4:** deixar rodando alguns dias. Revisão com o dono: 10–20 linhas do `.jsonl`. **Ativação real (enviar resposta + `proposal_draft`) é um plano separado, só depois desse ok.**

---

## Verificação final (antes de pedir merge de cada PR)

- `dotnet test api-core/tests/Diax.Tests/Diax.Tests.csproj` → tudo verde.
- `cd crm-web && npx vitest run && npm run build` → verde.
- Migration lida linha a linha (só `CreateTable`/`AddColumn`/`CreateIndex`).
- PR com descrição do que muda em produção (inclui "aplica migration no startup").
