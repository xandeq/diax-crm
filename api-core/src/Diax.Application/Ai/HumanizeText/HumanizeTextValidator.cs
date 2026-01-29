using FluentValidation;

namespace Diax.Application.Ai.HumanizeText;

public class HumanizeTextValidator : AbstractValidator<HumanizeTextRequestDto>
{
    private readonly string[] _allowedProviders = { "chatgpt", "perplexity", "deepseek" };
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
            .NotEmpty().WithMessage("O provedor de IA deve ser informado.")
            .Must(p => _allowedProviders.Contains(p.ToLower()))
            .WithMessage("Provedor inválido. Opções: ChatGPT, Perplexity, DeepSeek.");

        RuleFor(x => x.Tone)
            .NotEmpty().WithMessage("O tom deve ser informado.")
            .Must(t => _allowedTones.Contains(t.ToLower()))
            .WithMessage("Tom inválido.");
    }
}
