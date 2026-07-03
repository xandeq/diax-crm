using System.Collections.Concurrent;
using Diax.Application.AI.MediaStorage;
using Diax.Application.Customers;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Gera a capa da proposta via Pollinations (keyless, grátis) em background.
/// Fire-safe de verdade: Task.Run com ESCOPO PRÓPRIO (novo DbContext) e
/// try/catch total — falha vira log, nunca afeta a criação da proposta.
/// Dedup em memória evita gerações concorrentes do mesmo id.
/// </summary>
public class ProposalCoverImageGenerator : IProposalCoverImageGenerator
{
    private static readonly ConcurrentDictionary<Guid, byte> InFlight = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProposalCoverImageGenerator> _logger;

    public ProposalCoverImageGenerator(
        IServiceScopeFactory scopeFactory,
        ILogger<ProposalCoverImageGenerator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void QueueGeneration(Guid proposalId, string proposalTitle)
    {
        if (!InFlight.TryAdd(proposalId, 0))
            return; // já gerando

        _ = Task.Run(async () =>
        {
            try
            {
                await GenerateAsync(proposalId, proposalTitle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Capa da proposta {ProposalId}: geração falhou (proposta segue sem capa)", proposalId);
            }
            finally
            {
                InFlight.TryRemove(proposalId, out _);
            }
        });
    }

    private async Task GenerateAsync(Guid proposalId, string title)
    {
        var prompt = IProposalCoverImageGenerator.BuildPrompt(title);
        // seed determinístico pelo id → mesma proposta, mesma arte (e cache no Pollinations)
        var seed = Math.Abs(proposalId.GetHashCode() % 100000);
        var url = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString(prompt)}" +
                  $"?width=1200&height=400&nologo=true&seed={seed}";

        using var scope = _scopeFactory.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IGeneratedMediaStorageService>();
        var proposals = scope.ServiceProvider.GetRequiredService<IProposalRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // TrySaveImageAsync baixa a URL (Pollinations gera on-demand) e persiste localmente
        var savedUrl = await storage.TrySaveImageAsync(url, isBase64: false, mediaId: proposalId);
        if (savedUrl == null)
        {
            _logger.LogInformation("Capa da proposta {ProposalId}: Pollinations indisponível — sem capa", proposalId);
            return;
        }

        var proposal = await proposals.GetByIdAsync(proposalId);
        if (proposal == null) return;

        proposal.SetCoverImage(savedUrl);
        await proposals.UpdateAsync(proposal);
        await uow.SaveChangesAsync();

        _logger.LogInformation("Capa da proposta {ProposalId} gerada: {Url}", proposalId, savedUrl);
    }
}
