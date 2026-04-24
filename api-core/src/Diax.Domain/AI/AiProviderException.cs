namespace Diax.Domain.AI;

/// <summary>
/// Thrown when an AI provider call fails with a known error category.
/// Carries an ErrorCode that maps to a user-friendly frontend label.
/// </summary>
public class AiProviderException : InvalidOperationException
{
    public string ErrorCode { get; }

    public AiProviderException(string message, string errorCode, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}
