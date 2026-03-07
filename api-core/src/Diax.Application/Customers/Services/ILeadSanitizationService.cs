using Diax.Application.Customers.Dtos;

namespace Diax.Application.Customers.Services;

/// <summary>
/// Serviço de domínio/aplicação responsável por higienizar, validar e classificar dados de Leads
/// antes da persistência no banco de dados.
/// </summary>
public interface ILeadSanitizationService
{
    /// <summary>
    /// Processa e sanitiza os dados de um Lead em tempo de criação ou importação.
    /// Retorna um objeto contendo os dados limpos, flags de validação e classificação de qualidade.
    /// </summary>
    SanitizedLeadResult SanitizeAndClassify(RawLeadData rawData);
}

/// <summary>
/// DTO com os dados brutos de entrada do Lead.
/// </summary>
public record RawLeadData(
    string Name,
    string? Email,
    string? Phone,
    string? WhatsApp,
    string? CompanyName,
    string? Notes);
