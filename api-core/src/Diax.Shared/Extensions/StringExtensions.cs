namespace Diax.Shared.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value) =>
        string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpace(this string? value) =>
        string.IsNullOrWhiteSpace(value);

    public static bool HasValue(this string? value) =>
        !string.IsNullOrWhiteSpace(value);

    public static string ToSlug(this string value)
    {
        if (value.IsNullOrWhiteSpace())
            return string.Empty;

        return value
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
    }
}
