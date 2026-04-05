namespace Diax.Application.Finance.Dtos;

public record ImportFromSheetResult(
    int MatchedCards,
    int UnmatchedCards,
    List<ImportedCardResult> Results
);

public record ImportedCardResult(
    string SheetName,
    string? MatchedCrmName,
    decimal? Amount,
    bool Matched,
    string? Error
);
