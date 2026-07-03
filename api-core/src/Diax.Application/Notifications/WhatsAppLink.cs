namespace Diax.Application.Notifications;

/// <summary>
/// Gera links wa.me (click-to-chat) a partir de telefones brasileiros em
/// qualquer formato — para o dono abrir a conversa com 1 toque no Telegram.
/// </summary>
public static class WhatsAppLink
{
    /// <summary>
    /// Normaliza o telefone e retorna a URL wa.me, ou null se não parecer um
    /// telefone BR válido (10-13 dígitos após limpeza).
    /// </summary>
    public static string? From(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = digits.TrimStart('0');

        // Já tem DDI 55 (12-13 dígitos) → usa direto
        if (digits.StartsWith("55") && digits.Length is 12 or 13)
            return $"https://wa.me/{digits}";

        // DDD + número (10-11 dígitos) → prefixa 55
        if (digits.Length is 10 or 11)
            return $"https://wa.me/55{digits}";

        return null; // 0800, números curtos, lixo — sem link
    }

    /// <summary>Telefone como link HTML clicável (Telegram) — ou o texto puro escapado se inválido.</summary>
    public static string AsHtml(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        var esc = phone.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        var link = From(phone);
        return link == null ? esc : $"<a href=\"{link}\">{esc}</a>";
    }
}
