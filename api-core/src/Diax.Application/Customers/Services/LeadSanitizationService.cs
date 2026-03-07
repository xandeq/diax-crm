using Diax.Application.Customers.Dtos;
using Diax.Domain.Customers.Enums;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Diax.Application.Customers.Services;

public class LeadSanitizationService : ILeadSanitizationService
{
    // E-mails que são claramente de sistemas, suporte, genéricos. Usado para classificar o EmailType.
    private static readonly HashSet<string> GenericEmailPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "contato", "suporte", "financeiro", "admin", "vendas", "faturamento", "atendimento", "info", "hello", "comercial"
    };

    // Domínios conhecidos por serem temporários ou spam/lixo
    private static readonly HashSet<string> SuspiciousDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com", "10minutemail.com", "tempmail.com", "yopmail.com", "guerrillamail.com"
    };

    // Regex para validar formato básico do email (garante que não tem espaços, tem @ e domínio válido)
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public SanitizedLeadResult SanitizeAndClassify(RawLeadData rawData)
    {
        var result = new SanitizedLeadResult();

        // 1. Correção e Normalização Textual
        var cleanName = SanitizeText(rawData.Name);
        var cleanCompany = SanitizeText(rawData.CompanyName);
        var cleanNotes = SanitizeText(rawData.Notes, isMultiline: true); // Não passa TitleCase nas Notas
        var cleanPhone = CleanPhone(rawData.Phone);
        var cleanWhatsApp = CleanPhone(rawData.WhatsApp);

        // Se após a sanitização ficar sem nome... vamos retornar reject e tentar contornar?
        // Normalmente não deve ocorrer rejeição nesse nível, mas deixamos preparado.
        if (string.IsNullOrWhiteSpace(cleanName))
        {
            result.ShouldReject = true;
            result.RejectionReason = "Name is invalid or missing after sanitization.";
            return result;
        }

        result.Name = cleanName;
        result.CompanyName = cleanCompany;
        result.Notes = cleanNotes;
        result.Phone = cleanPhone;
        result.WhatsApp = cleanWhatsApp;

        // 2. Validação e Normalização do E-mail
        if (!string.IsNullOrWhiteSpace(rawData.Email))
        {
            var cleanedEmail = rawData.Email.Trim().ToLowerInvariant();

            // Corrige emails grudados (ex: `teste@empresa.com, contato@`)
            if (cleanedEmail.Contains(","))
            {
                cleanedEmail = cleanedEmail.Split(',')[0].Trim();
            }

            if (EmailRegex.IsMatch(cleanedEmail))
            {
                result.Email = cleanedEmail;
                result.IsEmailValid = true;

                // Extrair as partes
                var parts = cleanedEmail.Split('@');
                var prefix = parts[0];
                var domain = parts[1];

                // 3. Detecção de Domínio Suspeito
                if (SuspiciousDomains.Contains(domain))
                {
                    result.HasSuspiciousDomain = true;
                }

                // 4. Classificação do E-mail
                if (GenericEmailPrefixes.Contains(prefix))
                {
                    result.EmailType = EmailType.GenericCorporate;
                }
                else
                {
                    result.EmailType = EmailType.PersonalDirect;
                }
            }
            else
            {
                result.IsEmailValid = false;
                result.Email = null; // Email formato incorreto, apaga
            }
        }

        // 5. Determinação da Qualidade (Scoring Simples)
        int score = 0;

        if (result.IsEmailValid) score += 3;
        if (!string.IsNullOrWhiteSpace(result.Phone) || !string.IsNullOrWhiteSpace(result.WhatsApp)) score += 2;
        if (!string.IsNullOrWhiteSpace(result.CompanyName)) score += 2;
        if (!result.HasSuspiciousDomain && result.IsEmailValid) score += 1; // Bônus por email bom

        if (score >= 6) result.Quality = LeadQuality.High;
        else if (score >= 3) result.Quality = LeadQuality.Medium;
        else result.Quality = LeadQuality.Low;

        // 6. Elegibilidade para Campanhas
        // - Precisa ter um email válido
        // - Não pode ser um domínio suspeito
        // - Ter o nome mínimo preenchido (não ser "Unknown")
        result.IsEligibleForCampaigns =
            result.IsEmailValid &&
            !result.HasSuspiciousDomain &&
            result.Name.Length > 2;

        return result;
    }

    /// <summary>
    /// Limpa e aplica Title Case numa String para corrigir Caps Locks indesejados e encodes zoados.
    /// </summary>
    private static string SanitizeText(string? input, bool isMultiline = false)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Limpa espaços no inicio e fim e substitui múltiplos espaços internos
        var clean = Regex.Replace(input.Trim(), @"\s+", " ");

        // Em descrições (multiline), ignorar o title case
        if (!isMultiline)
        {
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            // O TitleCase do .NET não muda se a string for toda maiúscula antes, então forçamos para minúsculo
            clean = textInfo.ToTitleCase(clean.ToLowerInvariant());
        }

        return clean;
    }

    /// <summary>
    /// Limpa números de telefone.
    /// </summary>
    private static string? CleanPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var cleaned = Regex.Replace(phone.Trim(), @"[^\d+() -]", "");

        if (cleaned.Length > 50)
            cleaned = cleaned.Substring(0, 50).Trim();

        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }
}
