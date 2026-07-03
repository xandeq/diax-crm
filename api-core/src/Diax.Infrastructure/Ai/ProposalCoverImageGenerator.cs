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

        using var scope = _scopeFactory.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IGeneratedMediaStorageService>();
        var proposals = scope.ServiceProvider.GetRequiredService<IProposalRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Client Pollinations do módulo de imagens (baixa e retorna base64 — caminho já
        // comprovado em produção; o download direto pelo storage falhava)
        var pollinations = scope.ServiceProvider.GetRequiredService<PollinationsImageClient>();
        var results = await pollinations.GenerateAsync(prompt, new Diax.Shared.Ai.ImageGenerationOptions(
            ApiKey: string.Empty,
            BaseUrl: string.Empty,
            Model: "flux",
            Width: 1200,
            Height: 400,
            NumberOfImages: 1,
            Seed: seed.ToString()));
        if (results.Count == 0)
        {
            _logger.LogInformation("Capa da proposta {ProposalId}: Pollinations sem resultado — sem capa", proposalId);
            return;
        }

        var savedUrl = await storage.TrySaveImageAsync(results[0].ImageUrl, isBase64: true, mediaId: proposalId);
        if (savedUrl == null)
        {
            _logger.LogInformation("Capa da proposta {ProposalId}: storage falhou — sem capa", proposalId);
            return;
        }

        // Fora de request o storage devolve URL relativa (/generated-media/...) — perfeito:
        // o controller absolutiza. Se vier absoluta (contexto vivo), normaliza p/ relativa.
        var idx = savedUrl.IndexOf("/generated-media/", StringComparison.OrdinalIgnoreCase);
        if (idx > 0) savedUrl = savedUrl[idx..];

        var proposal = await proposals.GetByIdAsync(proposalId);
        if (proposal == null) return;

        proposal.SetCoverImage(savedUrl);
        await proposals.UpdateAsync(proposal);
        await uow.SaveChangesAsync();

        _logger.LogInformation("Capa da proposta {ProposalId} gerada: {Url}", proposalId, savedUrl);
    }
}
