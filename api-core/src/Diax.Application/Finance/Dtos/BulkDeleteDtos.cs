namespace Diax.Application.Finance.Dtos;

public record BulkDeleteRequest(List<Guid> Ids);

public record BulkDeleteResponse(
    bool Success,
    int DeletedCount,
    int FailedCount,
    List<string> Errors
);
