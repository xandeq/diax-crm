namespace Diax.Application.AI.FreeGateway;

/// <summary>Mensagem de chat no formato OpenAI (role/content).</summary>
public class FreeAiChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>Requisição de chat completion para o gateway de IAs gratuitas.</summary>
public class FreeAiChatRequest
{
    public List<FreeAiChatMessage> Messages { get; set; } = new();
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }

    /// <summary>
    /// null/"auto" = tenta a cadeia inteira na ordem configurada.
    /// "provider/model" = começa por esse par explícito, depois cai para o resto da cadeia.
    /// "model" (sem "/") = precisa existir em algum item da cadeia; começa por ele.
    /// </summary>
    public string? Model { get; set; }
}

/// <summary>Uma tentativa de chamada a um provedor da cadeia (sucesso ou falha).</summary>
public class FreeAiAttempt
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    /// <summary>"ok" ou um status/erro curto (ex.: "429", "timeout", "no_api_key", "empty_content").</summary>
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public long ElapsedMs { get; set; }
}

/// <summary>Resultado de <see cref="IFreeAiGatewayService.CompleteAsync"/>.</summary>
public class FreeAiChatResult
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public string? FinishReason { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public List<FreeAiAttempt> Attempts { get; set; } = new();
}

/// <summary>Resultado de um item em <see cref="IFreeAiGatewayService.CheckHealthAsync"/>.</summary>
public class FreeAiHealthItem
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool Ok { get; set; }
    public long ElapsedMs { get; set; }
    public string? Error { get; set; }
}

/// <summary>Resultado completo de <see cref="IFreeAiGatewayService.CheckHealthAsync"/>.</summary>
public class FreeAiHealthResult
{
    public List<FreeAiHealthItem> Items { get; set; } = new();
    public int OkCount => Items.Count(i => i.Ok);
    public int TotalCount => Items.Count;

    /// <summary>True quando o resultado veio do cache (não martelou os provedores de novo).</summary>
    public bool Cached { get; set; }

    /// <summary>Quando os provedores foram efetivamente checados (não quando essa resposta foi servida).</summary>
    public DateTimeOffset CheckedAt { get; set; }
}
