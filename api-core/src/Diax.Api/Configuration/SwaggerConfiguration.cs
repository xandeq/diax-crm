using Microsoft.OpenApi.Models;

namespace Diax.Api.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DIAX CRM - Core API",
                Version = "v1",
                Description = "API central do ecossistema DIAX CRM. Gerencia clientes, leads, projetos, financeiro e integrações.",
                Contact = new OpenApiContact
                {
                    Name = "DIAX",
                    Email = "contato@diax.com.br"
                }
            });

            // Tipos homônimos em namespaces diferentes (ex.: Finance.TransactionType e
            // Finance.Planner.TransactionType) colidiam no schemaId → swagger.json 500.
            // Nome curto quando é único; nome completo só para quem colide.
            options.CustomSchemaIds(SchemaIdFor);

            // Configuração para JWT (preparado para uso futuro)
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token JWT no formato: Bearer {seu_token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Type> _schemaIdOwners = new();

    /// <summary>Id padrão do Swashbuckle (nome curto, genéricos achatados); se outro tipo
    /// já ocupou esse id, usa o nome completo — evita a colisão sem renomear os demais schemas.</summary>
    internal static string SchemaIdFor(Type type)
    {
        var shortId = ShortName(type);
        var owner = _schemaIdOwners.GetOrAdd(shortId, type);
        return owner == type ? shortId : FullName(type);
    }

    private static string ShortName(Type t) => t.IsGenericType
        ? t.Name[..t.Name.IndexOf('`')] + "Of" + string.Join("And", t.GetGenericArguments().Select(ShortName))
        : t.Name;

    private static string FullName(Type t) => t.IsGenericType
        ? $"{t.Namespace}.{ShortName(t)}".Replace('+', '.')
        : (t.FullName ?? t.Name).Replace('+', '.');
}
