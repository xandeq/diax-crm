namespace Diax.Application.Ai.Services;

public interface ITokenEstimator
{
    int EstimateTokens(string text);
}

public class TokenEstimator : ITokenEstimator
{
    // Conservative estimate: 4 characters per token (GPT average)
    private const double CharsPerToken = 4.0;

    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Round up to be conservative with cost estimates
        return (int)Math.Ceiling(text.Length / CharsPerToken);
    }
}
