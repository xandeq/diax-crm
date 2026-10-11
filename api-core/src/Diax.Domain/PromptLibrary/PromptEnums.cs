namespace Diax.Domain.PromptLibrary;

/// <summary>Tipo do prompt na biblioteca.</summary>
public enum PromptKind
{
    Prompt = 0,
    Template = 1,
    System = 2
}

/// <summary>Origem de um prompt ou de uma versão.</summary>
public enum PromptSource
{
    Manual = 0,
    Generator = 1,
    Snippet = 2,
    Import = 3,
    Mcp = 4,
    Git = 5
}
