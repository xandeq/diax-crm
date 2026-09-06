using Diax.Domain.Common;
using Diax.Domain.Customers.Enums;

namespace Diax.Domain.Customers;

/// <summary>
/// Entidade que representa um histórico de importação de leads/customers.
/// Armazena metadados sobre a importação e seus resultados.
/// </summary>
public class CustomerImport : AuditableEntity
{
    /// <summary>
    /// Nome do arquivo importado (se CSV) ou identificador do texto colado.
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Tipo de importação (CSV ou Text).
    /// </summary>
    public ImportType Type { get; private set; }

    /// <summary>
    /// Status atual da importação.
    /// </summary>
    public ImportStatus Status { get; private set; }

    /// <summary>
    /// Total de registros tentados na importação.
    /// </summary>
    public int TotalRecords { get; private set; }

    /// <summary>
    /// Quantidade de registros importados com sucesso.
    /// </summary>
    public int SuccessCount { get; private set; }

    /// <summary>
    /// Quantidade de registros que falharam.
    /// </summary>
    public int FailedCount { get; private set; }

    /// <summary>
    /// Detalhes dos erros ocorridos (JSON array de erros).
    /// </summary>
    public string? ErrorDetails { get; private set; }

    // ===== CONTADORES DE REJEIÇÃO POR RODADA (EXTR-02 / decisão D-04) =====
    // Agregados, NÃO por lead: o banco está em ~467/500 MB de quota e ~900 leads/dia são
    // rejeitados — uma tabela por lead precisaria de purge desde o primeiro dia (D-05).
    // Consultável por período via o índice IX_CustomerImports_CreatedAt que já existe.

    /// <summary>Leads descartados por UF/DDD fora da região alvo.</summary>
    public int GeoRejectedCount { get; private set; }

    /// <summary>Leads descartados por e-mail lixo (domínio bloqueado, TLD estrangeiro, '%', domínio placeholder).</summary>
    public int LowQualityEmailRejectedCount { get; private set; }

    /// <summary>Leads descartados porque o domínio de e-mail comprovadamente não recebe e-mail (sem MX e sem A).</summary>
    public int NoMxRejectedCount { get; private set; }

    /// <summary>Leads que casaram com um Customer já existente (update, não create).</summary>
    public int DuplicateRejectedCount { get; private set; }

    // ===== CONSTRUTORES =====

    /// <summary>
    /// Construtor para EF Core.
    /// </summary>
    protected CustomerImport() { }

    /// <summary>
    /// Cria uma nova importação.
    /// </summary>
    /// <param name="fileName">Nome do arquivo ou identificador</param>
    /// <param name="type">Tipo de importação</param>
    /// <param name="totalRecords">Total de registros a processar</param>
    public CustomerImport(string fileName, ImportType type, int totalRecords)
    {
        FileName = fileName;
        Type = type;
        TotalRecords = totalRecords;
        Status = ImportStatus.Processing;
        SuccessCount = 0;
        FailedCount = 0;
        GeoRejectedCount = 0;
        LowQualityEmailRejectedCount = 0;
        NoMxRejectedCount = 0;
        DuplicateRejectedCount = 0;
    }

    // ===== MÉTODOS DE DOMÍNIO =====

    /// <summary>
    /// Registra os contadores agregados de rejeição desta rodada de import (EXTR-02).
    /// Não altera Status/SuccessCount/FailedCount — pode ser chamado antes ou depois de Complete.
    /// Valores negativos são normalizados para 0.
    /// </summary>
    public void RecordRejectionCounts(int geo, int lowQualityEmail, int noMx, int duplicate)
    {
        GeoRejectedCount = Math.Max(0, geo);
        LowQualityEmailRejectedCount = Math.Max(0, lowQualityEmail);
        NoMxRejectedCount = Math.Max(0, noMx);
        DuplicateRejectedCount = Math.Max(0, duplicate);
    }

    /// <summary>
    /// Marca a importação como concluída com os resultados.
    /// </summary>
    /// <param name="successCount">Quantidade de registros importados com sucesso</param>
    /// <param name="failedCount">Quantidade de registros que falharam</param>
    /// <param name="errors">Detalhes dos erros (JSON)</param>
    public void Complete(int successCount, int failedCount, string? errors)
    {
        SuccessCount = successCount;
        FailedCount = failedCount;
        ErrorDetails = errors;

        // Define o status baseado nos resultados
        if (failedCount == 0 && successCount > 0)
        {
            Status = ImportStatus.Completed;
        }
        else if (successCount == 0)
        {
            Status = ImportStatus.Failed;
        }
        else
        {
            Status = ImportStatus.PartialSuccess;
        }
    }

    /// <summary>
    /// Marca a importação como falhada com erro crítico.
    /// </summary>
    /// <param name="error">Mensagem de erro</param>
    public void Fail(string error)
    {
        Status = ImportStatus.Failed;
        ErrorDetails = error;
        FailedCount = TotalRecords;
        SuccessCount = 0;
    }
}
