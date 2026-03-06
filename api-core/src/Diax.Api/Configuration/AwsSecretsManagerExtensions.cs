using Amazon;
using Amazon.Extensions.NETCore.Setup;

namespace Diax.Api.Configuration;

/// <summary>
/// Extensão para integrar o AWS Secrets Manager ao pipeline de configuração do .NET.
/// Ativo apenas em ambientes não-Development.
/// Os secrets devem ser criados no AWS Secrets Manager com o path: /diax-crm/{Secao}/{Chave}
/// Exemplo: /diax-crm/Jwt/Key → Jwt:Key no IConfiguration
/// </summary>
public static class AwsSecretsManagerExtensions
{
    private const string SecretsPrefix = "/diax-crm/";

    public static IConfigurationBuilder AddAwsSecretsManager(
        this IConfigurationBuilder builder,
        IHostEnvironment environment)
    {
        // Ativado localmente e em produção, assume que a AWS CLI ou Environment Variables possuem as credenciais.

        try
        {
            var awsOptions = new AWSOptions
            {
                Region = RegionEndpoint.USEast1
            };

            // AddSystemsManager busca tanto SSM Parameter Store quanto Secrets Manager
            // dependendo do path. O pacote lida com a conversão de path → IConfiguration key.
            builder.AddSystemsManager(
                path: SecretsPrefix,
                awsOptions: awsOptions,
                optional: true,    // Não derruba o startup se o AWS estiver inacessível
                reloadAfter: TimeSpan.FromMinutes(10)
            );
        }
        catch (Exception ex)
        {
            // Log via Console pois o Serilog ainda não está configurado neste ponto
            Console.WriteLine($"[AWS SecretsManager] Falha ao configurar: {ex.Message}. " +
                              "A API usará as variáveis de ambiente como fallback.");
        }

        return builder;
    }
}
