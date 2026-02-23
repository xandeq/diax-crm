using System.Text;
using System.Text.Json;

namespace Diax.Application.AI.ImageGeneration;

public static class ImagePromptBuilder
{
    /// <summary>
    /// Builds a final prompt by merging the template placeholders with user parameters.
    /// If no template is provided, returns the raw prompt.
    /// </summary>
    public static string Build(string rawPrompt, string? templatePrompt, string? parametersJson)
    {
        if (string.IsNullOrWhiteSpace(templatePrompt))
            return rawPrompt;

        var result = templatePrompt;

        // Replace {prompt} placeholder with user's raw prompt
        result = result.Replace("{prompt}", rawPrompt, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(parametersJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(parametersJson);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var placeholder = $"{{{prop.Name}}}";
                    var value = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString() ?? string.Empty
                        : prop.Value.ToString();
                    result = result.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                // If JSON parsing fails, just use the template with the raw prompt
            }
        }

        return result;
    }
}
