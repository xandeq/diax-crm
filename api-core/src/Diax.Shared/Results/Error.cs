namespace Diax.Shared.Results;

/// <summary>
/// Representa um erro de domínio ou aplicação.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "O valor fornecido é nulo.");

    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"{entity} com identificador '{id}' não foi encontrado.");

    public static Error Validation(string code, string message) =>
        new($"Validation.{code}", message);

    public static Error Conflict(string code, string message) =>
        new($"Conflict.{code}", message);

    public static Error Unauthorized(string message = "Acesso não autorizado.") =>
        new("Error.Unauthorized", message);

    public static Error Forbidden(string message = "Acesso negado.") =>
        new("Error.Forbidden", message);
}
