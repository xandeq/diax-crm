namespace Diax.Application.AI.Dtos;

public record GrokModelsResponse(
    string Object,
    List<GrokModel> Data
);

public record GrokModel(
    string Id,
    string Object,
    long Created,
    string OwnedBy
);
