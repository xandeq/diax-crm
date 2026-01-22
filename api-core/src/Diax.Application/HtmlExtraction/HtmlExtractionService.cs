using Diax.Application.Common;
using Diax.Application.HtmlExtraction.Dtos;
using Diax.Shared.Results;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Diax.Application.HtmlExtraction;

public class HtmlExtractionService : IApplicationService
{
    private readonly ILogger<HtmlExtractionService> _logger;

    public HtmlExtractionService(ILogger<HtmlExtractionService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<ExtractTextResponse>> ExtractTextAsync(ExtractTextRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting HTML text extraction");

            if (string.IsNullOrWhiteSpace(request.Html))
            {
                _logger.LogWarning("HTML extraction failed: Empty or null HTML provided");
                return Result.Failure<ExtractTextResponse>(
                    new Error("HtmlExtraction.InvalidInput", "HTML content cannot be empty"));
            }

            // Limit HTML size to 5MB
            if (request.Html.Length > 5 * 1024 * 1024)
            {
                _logger.LogWarning("HTML extraction failed: HTML size exceeds 5MB limit");
                return Result.Failure<ExtractTextResponse>(
                    new Error("HtmlExtraction.TooLarge", "HTML content exceeds maximum size of 5MB"));
            }

            var extractedText = await Task.Run(() => ExtractVisibleText(request.Html), cancellationToken);

            _logger.LogInformation("Successfully extracted text from HTML ({Length} characters)", extractedText.Length);

            return Result<ExtractTextResponse>.Success(new ExtractTextResponse(extractedText));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from HTML");
            return Result.Failure<ExtractTextResponse>(
                new Error("HtmlExtraction.ProcessingFailed", "Failed to extract text from HTML. Please check server logs for details."));
        }
    }

    public async Task<Result<ExtractUrlsResponse>> ExtractUrlsAsync(ExtractUrlsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting HTML URL extraction");

            if (string.IsNullOrWhiteSpace(request.Html))
            {
                _logger.LogWarning("HTML URL extraction failed: Empty or null HTML provided");
                return Result.Failure<ExtractUrlsResponse>(
                    new Error("HtmlExtraction.InvalidInput", "HTML content cannot be empty"));
            }

            // Limit HTML size to 5MB
            if (request.Html.Length > 5 * 1024 * 1024)
            {
                _logger.LogWarning("HTML URL extraction failed: HTML size exceeds 5MB limit");
                return Result.Failure<ExtractUrlsResponse>(
                    new Error("HtmlExtraction.TooLarge", "HTML content exceeds maximum size of 5MB"));
            }

            var urls = await Task.Run(() => ExtractUrls(request.Html), cancellationToken);

            _logger.LogInformation("Successfully extracted {Count} URLs from HTML", urls.Count);

            return Result<ExtractUrlsResponse>.Success(new ExtractUrlsResponse(urls));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract URLs from HTML");
            return Result.Failure<ExtractUrlsResponse>(
                new Error("HtmlExtraction.ProcessingFailed", "Failed to extract URLs from HTML. Please check server logs for details."));
        }
    }

    private string ExtractVisibleText(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove script tags
        var scriptNodes = doc.DocumentNode.SelectNodes("//script");
        if (scriptNodes != null)
        {
            foreach (var node in scriptNodes)
            {
                node.Remove();
            }
        }

        // Remove style tags
        var styleNodes = doc.DocumentNode.SelectNodes("//style");
        if (styleNodes != null)
        {
            foreach (var node in styleNodes)
            {
                node.Remove();
            }
        }

        // Remove head tag
        var headNode = doc.DocumentNode.SelectSingleNode("//head");
        if (headNode != null)
        {
            headNode.Remove();
        }

        // Remove HTML comments
        var commentNodes = doc.DocumentNode.SelectNodes("//comment()");
        if (commentNodes != null)
        {
            foreach (var node in commentNodes)
            {
                node.Remove();
            }
        }

        // Extract inner text
        var text = doc.DocumentNode.InnerText;

        // Decode HTML entities
        text = HtmlEntity.DeEntitize(text);

        // Clean up whitespace
        text = CleanWhitespace(text);

        return text.Trim();
    }

    private string CleanWhitespace(string text)
    {
        // Replace multiple whitespace characters with single space
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var cleanedLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                // Replace multiple spaces with single space
                var cleaned = System.Text.RegularExpressions.Regex.Replace(trimmedLine, @"\s+", " ");
                cleanedLines.Add(cleaned);
            }
        }

        return string.Join("\n", cleanedLines);
    }

    private IReadOnlyList<string> ExtractUrls(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nodes = doc.DocumentNode.SelectNodes("//*[@href or @src or @xlink:href or @data-href or @data-src or @poster or @srcset or @style]");
        if (nodes == null || nodes.Count == 0)
        {
            return Array.Empty<string>();
        }

        var urls = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes)
        {
            var href = node.GetAttributeValue("href", null);
            var src = node.GetAttributeValue("src", null);
            var xlinkHref = node.GetAttributeValue("xlink:href", null);
            var dataHref = node.GetAttributeValue("data-href", null);
            var dataSrc = node.GetAttributeValue("data-src", null);
            var poster = node.GetAttributeValue("poster", null);
            var srcset = node.GetAttributeValue("srcset", null);
            var style = node.GetAttributeValue("style", null);

            AddIfValidUrl(href, urls, seen);
            AddIfValidUrl(src, urls, seen);
            AddIfValidUrl(xlinkHref, urls, seen);
            AddIfValidUrl(dataHref, urls, seen);
            AddIfValidUrl(dataSrc, urls, seen);
            AddIfValidUrl(poster, urls, seen);
            AddSrcSetUrls(srcset, urls, seen);
            AddStyleUrls(style, urls, seen);
        }

        return urls;
    }

    private static void AddIfValidUrl(string? rawValue, ICollection<string> urls, ISet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        var value = HtmlEntity.DeEntitize(rawValue).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        value = NormalizeUrlValue(value);

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!IsValidUrlFormat(value))
        {
            return;
        }

        if (IsBlockedHost(value))
        {
            return;
        }

        if (seen.Add(value))
        {
            urls.Add(value);
        }
    }

    private static bool IsValidUrlFormat(string value)
    {
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme is "http" or "https";
        }

        if (Uri.TryCreate(value, UriKind.Relative, out _))
        {
            return value.StartsWith("/", StringComparison.Ordinal)
                || value.StartsWith("./", StringComparison.Ordinal)
                || value.StartsWith("../", StringComparison.Ordinal);
        }

        return false;
    }

    private static bool IsBlockedHost(string value)
    {
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            value = "https:" + value;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
        {
            return false;
        }

        var host = absoluteUri.Host;
        return host.Equals("google.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("www.google.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeUrlValue(string value)
    {
        var hashIndex = value.IndexOf('#');
        if (hashIndex >= 0)
        {
            value = value[..hashIndex];
        }

        return value.Trim();
    }

    private static void AddSrcSetUrls(string? srcset, ICollection<string> urls, ISet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(srcset))
        {
            return;
        }

        var parts = srcset.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var candidate = part.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var spaceIndex = candidate.IndexOf(' ');
            var url = spaceIndex > 0 ? candidate[..spaceIndex] : candidate;
            AddIfValidUrl(url, urls, seen);
        }
    }

    private static void AddStyleUrls(string? style, ICollection<string> urls, ISet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return;
        }

        var matches = System.Text.RegularExpressions.Regex.Matches(style, "url\\(([^)]+)\\)");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Groups.Count < 2)
            {
                continue;
            }

            var raw = match.Groups[1].Value.Trim().Trim('"', '\'');
            AddIfValidUrl(raw, urls, seen);
        }
    }
}
