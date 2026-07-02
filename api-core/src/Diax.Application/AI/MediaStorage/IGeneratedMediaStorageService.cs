namespace Diax.Application.AI.MediaStorage;

/// <summary>
/// Persiste mídia gerada por IA (imagens) em storage próprio, produzindo URLs duráveis.
/// URLs de providers (DALL·E, FAL) expiram em horas; base64 não cabe no banco —
/// este serviço resolve os dois casos salvando o binário localmente (wwwroot).
/// </summary>
public interface IGeneratedMediaStorageService
{
    /// <summary>
    /// Tenta persistir uma imagem gerada e retorna a URL pública durável.
    /// Aceita: data URI ("data:image/png;base64,..."), base64 puro ou URL http(s) do provider
    /// (nesse caso o binário é baixado). NUNCA lança exceção — retorna null em falha
    /// (o chamador mantém a URL do provider ou entrega o base64 só na resposta).
    /// </summary>
    /// <param name="imageUrlOrBase64">Conteúdo retornado pelo client de geração.</param>
    /// <param name="isBase64">True quando o client marcou o resultado como base64/data URI.</param>
    /// <param name="mediaId">ID da entidade GeneratedImage — usado como nome do arquivo.</param>
    Task<string?> TrySaveImageAsync(
        string imageUrlOrBase64,
        bool isBase64,
        Guid mediaId,
        CancellationToken ct = default);
}
