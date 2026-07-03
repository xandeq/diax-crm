namespace Diax.Application.Customers;

/// <summary>
/// Gera a capa da proposta com IA em BACKGROUND (fire-safe): a criação da
/// proposta nunca espera nem falha por causa da imagem.
/// </summary>
public interface IProposalCoverImageGenerator
{
    /// <summary>Enfileira a geração da capa. Retorna imediatamente.</summary>
    void QueueGeneration(Guid proposalId, string proposalTitle);

    /// <summary>Prompt visual a partir do título — sem texto na imagem (IA gera gibberish).</summary>
    static string BuildPrompt(string title) =>
        $"professional abstract hero banner for business proposal about {title}, " +
        "modern purple and dark gradient, clean minimal geometric shapes, " +
        "high quality, no text, no letters, no words";
}
