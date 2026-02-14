using Diax.Application.Blog.Dtos;
using FluentValidation;

namespace Diax.Application.Blog.Validators;

public class UpdateBlogPostValidator : AbstractValidator<UpdateBlogPostRequest>
{
    public UpdateBlogPostValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Título é obrigatório.")
            .MaximumLength(200).WithMessage("Título deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Slug)
            .MaximumLength(250).WithMessage("Slug deve ter no máximo 250 caracteres.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug deve conter apenas letras minúsculas, números e hífens.")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.ContentHtml)
            .NotEmpty().WithMessage("Conteúdo é obrigatório.");

        RuleFor(x => x.Excerpt)
            .NotEmpty().WithMessage("Resumo é obrigatório.")
            .MaximumLength(500).WithMessage("Resumo deve ter no máximo 500 caracteres.");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(70).WithMessage("Meta Title deve ter no máximo 70 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.MetaTitle));

        RuleFor(x => x.MetaDescription)
            .MaximumLength(160).WithMessage("Meta Description deve ter no máximo 160 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.MetaDescription));

        RuleFor(x => x.Keywords)
            .MaximumLength(500).WithMessage("Keywords deve ter no máximo 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Keywords));

        RuleFor(x => x.FeaturedImageUrl)
            .MaximumLength(500).WithMessage("URL da imagem deve ter no máximo 500 caracteres.")
            .Must(BeAValidUrl).WithMessage("URL da imagem deve ser válida.")
            .When(x => !string.IsNullOrEmpty(x.FeaturedImageUrl));

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Categoria deve ter no máximo 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.Tags)
            .MaximumLength(500).WithMessage("Tags devem ter no máximo 500 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Tags));
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
