namespace Diax.Application.Ai.HumanizeText;

public static class HumanizeTextPromptBuilder
{
    private const string BaseSystemPrompt =
        "Você é um especialista em redação e comunicação. Sua tarefa é humanizar o texto fornecido pelo usuário.\n" +
        "Regras obrigatórias:\n" +
        "- Use linguagem simples, natural e objetiva.\n" +
        "- Varie o tamanho das frases para criar um ritmo fluido.\n" +
        "- Evite clichês, jargões excessivos e introduções/conclusões artificiais (ex: 'Aqui está o texto...', 'Em conclusão').\n" +
        "- NÃO invente conteúdo. Mantenha todas as informações relevantes do texto original.\n" +
        "- NÃO remova dados importantes (datas, valores, nomes).\n" +
        "- NÃO explique o que foi feito ou as alterações realizadas. Retorne APENAS o texto humanizado.\n" +
        "- Idioma: {0}.\n";

    public static string BuildSystemPrompt(string tone, string language = "pt-BR")
    {
        var basePrompt = string.Format(BaseSystemPrompt, language);

        var tonePrompt = tone.ToLower() switch
        {
            "humanize_text_light" =>
                "Tom: Leve e Casual.\n" +
                "Instrução: Escreva de forma descontraída, como se estivesse conversando com um amigo, mas mantendo o respeito. Evite formalismos desnecessários.",

            "humanize_text_professional" =>
                "Tom: Profissional e Corporativo.\n" +
                "Instrução: Use um tom sério, polido e direto. Ideal para e-mails de trabalho ou comunicações oficiais. Mantenha a elegância sem ser pomposo.",

            "humanize_text_marketing" =>
                "Tom: Marketing e Persuasivo.\n" +
                "Instrução: Foque no engajamento e na clareza. Use uma linguagem que desperte interesse, mas sem parecer 'vendedor de promessas vazias'. Seja convincente e natural.",

            "humanize_text_documentation" =>
                "Tom: Técnico e Documental.\n" +
                "Instrução: Priorize a precisão, clareza e organização. Organize as ideias de forma lógica. Ideal para Manuais, Wikis ou documentação técnica.",

            _ => "Tom: Neutro e Natural."
        };

        return $"{basePrompt}\n{tonePrompt}";
    }

    public static string BuildUserPrompt(string inputText)
    {
        return $"Texto para humanizar:\n{inputText}";
    }
}
