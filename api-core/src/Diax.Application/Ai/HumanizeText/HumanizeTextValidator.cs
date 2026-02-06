using FluentValidation;

namespace Diax.Application.Ai.HumanizeText;

/// <summary>
/// Validador de request para humanização de texto.
/// NOTA: A validação do provider é feita no HumanizeTextService consultando o banco de dados.
/// Este validator contém apenas validações síncronas básicas.
/// </summary>
public class HumanizeTextValidator : AbstractValidator<HumanizeTextRequestDto>
{
    private readonly string[] _allowedTones =
    {
        "humanize_text_light",
        "humanize_text_professional",
        "humanize_text_marketing",
        "humanize_text_documentation"
    };

    public HumanizeTextValidator()
    {
        RuleFor(x => x.InputText)
            .NotEmpty().WithMessage("O texto de entrada não pode ser vazio.")
            .MaximumLength(20000).WithMessage("O texto de entrada deve ter no máximo 20.000 caracteres.");

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("O provedor de IA deve ser informado.");
        // Validação do provider contra o banco é feita no Service (async)

        RuleFor(x => x.Tone)
            .NotEmpty().WithMessage("O tom deve ser informado.")
            .Must(t => _allowedTones.Contains(t.ToLower()))
            .WithMessage("Tom inválido.");
    }
}
