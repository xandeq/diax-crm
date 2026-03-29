using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ai;

/// <summary>
/// Image generation client for Hugging Face Inference Router API.
/// Calls POST https://router.huggingface.co/hf-inference/models/{model_id}
/// Auth: Authorization: Bearer {hf_token}
/// Response: binary PNG converted to base64 data URL.
/// Docs: https://huggingface.co/docs/api-inference/tasks/text-to-image
/// </summary>
public class HuggingFaceImageClient : IAiImageGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HuggingFaceImageClient> _logger;

    public string ProviderName => "huggingface";
    public bool SupportsImageToImage => false;

    private static readonly HashSet<string> StructuredInputModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "black-forest-labs/FLUX.1-schnell",
        "black-forest-labs/FLUX.1-dev",
        "stabilityai/stable-diffusion-3-medium-diffusers",
        "stabilityai/stable-diffusion-xl-base-1.0",
    };

    public HuggingFaceImageClient(HttpClient httpClient, ILogger<HuggingFaceImageClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
        _logger = logger;
    }

    public async Task<List<ImageGenerationResult>> GenerateAsync(
        string? prompt,
        ImageGenerationOptions options,
        string? referenceImageBase64 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("API key not configured for Hugging Face.");

        var modelId = options.Model;
        var endpoint = $"https://router.huggingface.co/hf-inference/models/{modelId}";

        var payload = new Dictionary<string, object>
        {
            ["inputs"] = prompt ?? string.Empty
        };

        if (StructuredInputModels.Contains(modelId))
        {
            var parameters = new Dictionary<string, object>();
            if (options.Width > 0 && options.Height > 0)
            {
                parameters["width"] = options.Width;
                parameters["height"] = options.Height;
            }

            if (!string.IsNullOrWhiteSpace(options.NegativePrompt))
                parameters["negative_prompt"] = options.NegativePrompt;

            if (parameters.Count > 0)
                payload["parameters"] = parameters;
        }

        var json = JsonSerializer.Serialize(payload);
        _logger.LogInformation("[HuggingFace] Generating image: model={Model}", modelId);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "[HuggingFace] Timeout gerando imagem com modelo {Model}", modelId);
            throw new InvalidOperationException(
                $"Timeout ao gerar imagem com HuggingFace ({_httpClient.Timeout.TotalSeconds}s). Modelos gratuitos podem demorar em cold start; tente novamente em instantes.");
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("[HuggingFace] Error {StatusCode} for model {Model}: {Body}", (int)response.StatusCode, modelId, errorBody);

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                string? estimatedTime = null;
                try
                {
                    using var errDoc = JsonDocument.Parse(errorBody);
                    if (errDoc.RootElement.TryGetProperty("estimated_time", out var et))
                        estimatedTime = $" ({et.GetDouble():F0}s estimados)";
                }
                catch
                {
                }

                throw new InvalidOperationException(
                    $"Modelo '{modelId}' está carregando no HuggingFace{estimatedTime}. Isso é normal na primeira chamada (cold start gratuito); aguarde alguns segundos e tente novamente.");
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new InvalidOperationException(
                    $"Rate limit do HuggingFace atingido para o modelo '{modelId}'. Aguarde 60s e tente novamente, ou troque para outro modelo. Causa: {errorBody}");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException(BuildAuthorizationErrorMessage(modelId, response.StatusCode, errorBody));
            }

            throw new InvalidOperationException(
                $"Erro ao gerar imagem com HuggingFace (modelo: {modelId}). Status: {(int)response.StatusCode}. Causa: {errorBody}");
        }

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            var imageBytes = await response.Content.ReadAsByteArrayAsync(ct);
            var base64 = Convert.ToBase64String(imageBytes);
            _logger.LogInformation("[HuggingFace] Image generated ({Bytes} bytes)", imageBytes.Length);
            return new List<ImageGenerationResult>
            {
                new(base64, true, null, null)
            };
        }

        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return ParseJsonResponse(body);
        }

        var rawBytes = await response.Content.ReadAsByteArrayAsync(ct);
        var rawBase64 = Convert.ToBase64String(rawBytes);
        return new List<ImageGenerationResult> { new(rawBase64, true, null, null) };
    }

    private static string BuildAuthorizationErrorMessage(string modelId, HttpStatusCode statusCode, string errorBody)
    {
        var normalizedError = ExtractNormalizedError(errorBody);

        if (statusCode == HttpStatusCode.Unauthorized)
        {
            return
                $"Token do HuggingFace inválido ou expirado para o modelo '{modelId}'. Atualize o token em Administração > AI > Providers e confirme que ele possui permissão de Inference API.";
        }

        if (LooksLikeGatedModelAccessIssue(normalizedError))
        {
            return
                $"O token do HuggingFace está autenticado, mas não tem acesso ao modelo gated '{modelId}'. Aceite os termos/licença do modelo na conta do HuggingFace e confirme que o token tem permissão de inferência.";
        }

        if (LooksLikeScopePermissionIssue(normalizedError))
        {
            return
                $"O token do HuggingFace não possui permissão suficiente para usar o modelo '{modelId}'. Revise os scopes/permissões do token em Administração > AI > Providers e gere um novo token com acesso à Inference API se necessário.";
        }

        return
            $"Falha de autorização no HuggingFace para o modelo '{modelId}'. Verifique se o token é válido, possui acesso à Inference API e se a conta aceitou os termos do modelo. Detalhe retornado: {normalizedError}";
    }

    private static string ExtractNormalizedError(string errorBody)
    {
        if (string.IsNullOrWhiteSpace(errorBody))
            return "sem detalhes retornados pelo provedor";

        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "error", out var error))
                    return error;

                if (TryGetString(root, "message", out var message))
                    return message;

                if (TryGetString(root, "detail", out var detail))
                    return detail;
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        if (TryGetString(item, "error", out var arrayError))
                            return arrayError;

                        if (TryGetString(item, "message", out var arrayMessage))
                            return arrayMessage;
                    }
                }
            }
        }
        catch
        {
        }

        return errorBody.Length > 300
            ? errorBody[..300] + "..."
            : errorBody;
    }

    private static bool LooksLikeGatedModelAccessIssue(string error)
    {
        var normalized = error.ToLowerInvariant();
        return normalized.Contains("gated")
            || normalized.Contains("license")
            || normalized.Contains("accept")
            || normalized.Contains("terms")
            || normalized.Contains("repository not found")
            || normalized.Contains("access to model")
            || normalized.Contains("restricted")
            || normalized.Contains("approval");
    }

    private static bool LooksLikeScopePermissionIssue(string error)
    {
        var normalized = error.ToLowerInvariant();
        return normalized.Contains("insufficient")
            || normalized.Contains("permission")
            || normalized.Contains("scope")
            || normalized.Contains("forbidden")
            || normalized.Contains("authorization");
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        value = string.Empty;
        return false;
    }

    private static List<ImageGenerationResult> ParseJsonResponse(string body)
    {
        var results = new List<ImageGenerationResult>();
        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("image", out var imgProp))
                {
                    var b64 = imgProp.GetString();
                    if (!string.IsNullOrEmpty(b64))
                        results.Add(new ImageGenerationResult(b64, true, null, null));
                }
            }
        }

        return results;
    }
}
