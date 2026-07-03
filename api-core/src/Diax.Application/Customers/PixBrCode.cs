using System.Globalization;
using System.Text;

namespace Diax.Application.Customers;

/// <summary>
/// Gerador de PIX copia-e-cola (BR Code estático, padrão EMV do Bacen).
/// 100% local — sem gateway: chave PIX + valor + nome/cidade → payload com CRC16.
/// </summary>
public static class PixBrCode
{
    /// <summary>
    /// Monta o payload "copia e cola" de um PIX estático.
    /// </summary>
    /// <param name="pixKey">Chave PIX (CPF/CNPJ/email/telefone/aleatória).</param>
    /// <param name="amount">Valor em R$ (2 casas).</param>
    /// <param name="merchantName">Nome do recebedor (máx 25 chars, sem acentos).</param>
    /// <param name="merchantCity">Cidade (máx 15 chars, sem acentos).</param>
    /// <param name="txId">Identificador da transação (máx 25 chars alfanuméricos) — ex.: id curto da proposta.</param>
    public static string Generate(string pixKey, decimal amount, string merchantName, string merchantCity, string txId)
    {
        if (string.IsNullOrWhiteSpace(pixKey))
            throw new ArgumentException("Chave PIX é obrigatória.");
        if (amount <= 0)
            throw new ArgumentException("Valor deve ser maior que zero.");

        var name = Sanitize(merchantName, 25);
        var city = Sanitize(merchantCity, 15);
        var tx = SanitizeTxId(txId, 25);
        var amountStr = amount.ToString("0.00", CultureInfo.InvariantCulture);

        var merchantAccount = Field("00", "br.gov.bcb.pix") + Field("01", pixKey.Trim());
        var additionalData = Field("05", tx);

        var payload = new StringBuilder()
            .Append(Field("00", "01"))                 // Payload Format Indicator
            .Append(Field("26", merchantAccount))      // Merchant Account Info (PIX)
            .Append(Field("52", "0000"))               // Merchant Category Code
            .Append(Field("53", "986"))                // Moeda (BRL)
            .Append(Field("54", amountStr))            // Valor
            .Append(Field("58", "BR"))                 // País
            .Append(Field("59", name))                 // Nome do recebedor
            .Append(Field("60", city))                 // Cidade
            .Append(Field("62", additionalData))       // TxId
            .Append("6304")                            // CRC (placeholder do próprio campo)
            .ToString();

        return payload + Crc16Ccitt(payload);
    }

    private static string Field(string id, string value)
        => id + value.Length.ToString("00") + value;

    /// <summary>Remove acentos, restringe a ASCII imprimível e aplica limite/uppercase.</summary>
    private static string Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return "N/A";

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (ch is >= ' ' and <= '~') sb.Append(ch);
        }

        var clean = sb.ToString().Trim().ToUpperInvariant();
        if (clean.Length == 0) return "N/A";
        return clean.Length <= maxLength ? clean : clean[..maxLength];
    }

    private static string SanitizeTxId(string? txId, int maxLength)
    {
        var clean = new string((txId ?? "").Where(char.IsLetterOrDigit).ToArray());
        if (clean.Length == 0) return "***";
        return clean.Length <= maxLength ? clean : clean[..maxLength];
    }

    /// <summary>CRC16-CCITT (poly 0x1021, init 0xFFFF) em hex maiúsculo — exigido pelo BR Code.</summary>
    public static string Crc16Ccitt(string payload)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.UTF8.GetBytes(payload))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc.ToString("X4");
    }
}
