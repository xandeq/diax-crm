using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Diax.Domain.Blog;

namespace Diax.Application.Blog.Services;

public interface ISlugGeneratorService
{
    Task<string> GenerateUniqueSlugAsync(
        string title,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}

public class SlugGeneratorService : ISlugGeneratorService
{
    private readonly IBlogPostRepository _repository;

    public SlugGeneratorService(IBlogPostRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> GenerateUniqueSlugAsync(
        string title,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var baseSlug = NormalizeSlug(title);
        var slug = baseSlug;
        var counter = 1;

        // Verificar se slug já existe, se sim, adicionar sufixo numérico
        while (await _repository.SlugExistsAsync(slug, excludeId, cancellationToken))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }

    private static string NormalizeSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Remove acentos
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var slug = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // Lowercase
        slug = slug.ToLowerInvariant();

        // Substituir espaços e underscores por hífens
        slug = Regex.Replace(slug, @"[\s_]+", "-");

        // Remover caracteres especiais (manter apenas a-z, 0-9, -)
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Remover hífens duplicados
        slug = Regex.Replace(slug, @"-+", "-");

        // Remover hífens no início e fim
        slug = slug.Trim('-');

        // Garantir que não fique vazio
        if (string.IsNullOrWhiteSpace(slug))
            slug = Guid.NewGuid().ToString("N").Substring(0, 8);

        return slug;
    }
}
