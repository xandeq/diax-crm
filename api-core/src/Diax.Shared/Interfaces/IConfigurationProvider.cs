namespace Diax.Shared.Interfaces;

public interface IConfigurationProvider
{
    Task<Result<(string url, string token)>> GetExtractorConfigAsync();
    string GetConfigSource();
}
