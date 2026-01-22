namespace Diax.Application.HtmlExtraction.Dtos;

public record ExtractUrlsResponse(
    IReadOnlyList<string> Urls
);
