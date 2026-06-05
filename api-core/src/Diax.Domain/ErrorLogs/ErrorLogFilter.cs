namespace Diax.Domain.ErrorLogs;

public record ErrorLogFilter(
    string? AppName,
    ErrorLogLevel? Level,
    bool? IsResolved,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search,
    string? Cursor,
    int Limit = 50)
{
    /// <summary>Codifica (occurredAt, id) em cursor opaco base64 estável.</summary>
    public static string EncodeCursor(DateTime occurredAt, Guid id)
    {
        var raw = $"{occurredAt.ToUniversalTime():O}|{id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    /// <summary>Decodifica cursor. Retorna null se inválido.</summary>
    public static (DateTime occurredAt, Guid id)? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;
        try
        {
            var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|', 2);
            if (parts.Length != 2) return null;
            if (!DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) return null;
            if (!Guid.TryParse(parts[1], out var id)) return null;
            return (dt, id);
        }
        catch { return null; }
    }
}
