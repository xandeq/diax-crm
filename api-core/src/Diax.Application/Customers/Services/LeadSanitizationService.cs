using Diax.Application.Customers.Dtos;
using Diax.Domain.Customers.Enums;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Diax.Application.Customers.Services;

public class LeadSanitizationService : ILeadSanitizationService
{
    private static readonly HashSet<string> GenericEmailPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "contato", "suporte", "financeiro", "admin", "vendas", "faturamento", "atendimento", "info", "hello", "comercial",
        "sac", "ouvidoria", "marketing", "rh", "vagas", "privacidade", "franquia", "faleconosco", "webmaster", "contact",
        "hello", "jobs", "press", "billing"
    };

    // Palavras promocionais para limpeza de nome (Case Insensitive)
    private static readonly HashSet<string> PromotionalKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Compre Agora", "Frete Grátis", "Arrase no Verão", "Desconto", "Promoção", "Promo", "Oferta",
        "Peça Já", "Entrega Rápida", "Mais Vendido", "Clique Aqui"
    };

    // Domínios conhecidos por serem temporários ou spam/lixo
    private static readonly HashSet<string> SuspiciousDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com", "10minutemail.com", "tempmail.com", "yopmail.com", "guerrillamail.com",
        "temp-mail.org", "throwawaymail.com", "fakeinbox.com", "dropmail.me", "fakemail.net", "getnada.com"
    };

    // Sufixos muito estranhos ou não comerciais comuns.
    private static readonly HashSet<string> SuspiciousDomainSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xyz", ".top", ".club", ".space", ".online", ".site", ".ru", ".tk"
    };

    // Palavras-chave que indicam diretórios ou páginas genéricas
    private static readonly HashSet<string> GenericCompanyKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Linktree", "404", "Not Found", "GuiaMais", "Telelistas", "Home", "Página Inicial", "Instagram", "Facebook",
        "LinkedIn", "Twitter", "TikTok", "YouTube", "Google", "WhatsApp", "Diretório", "Busca", "Erro 404",
        "Default", "Sem Nome", "Site", "Contato"
    };

    // Regex mais severo (rejeita espaços, obriga TLD válido simples com ao menos 2 letras)
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z]{2,})+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public SanitizedLeadResult SanitizeAndClassify(RawLeadData rawData)
    {
        var result = new SanitizedLeadResult();

        // Regra Especial de Diretório: Se o Nome ou Nome da Empresa batem exatamente ou continerem os termos genéricos ANTES da Limpeza Severa.
        bool isDirectoryOrGeneric = false;
        var checkName = rawData.CompanyName ?? rawData.Name;
        if (!string.IsNullOrWhiteSpace(checkName))
        {
            if (GenericCompanyKeywords.Any(k => checkName.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                isDirectoryOrGeneric = true;
                result.ShouldReject = true; // Por enquanto podemos apenas sinalizar o Reject. O CustomerService vai decidir o que fazer no batch.
                                            // Se no CreateManual cair aqui, rejeita direto.
                result.RejectionReason = "Lead originates from a Generic Directory/Profile (e.g., Linktree, GuiaMais) rather than real business context.";
            }
        }

        // 1. Correção e Normalização Textual
        var cleanName = SanitizeText(rawData.Name);
        var cleanCompany = SanitizeText(rawData.CompanyName);
        var cleanNotes = SanitizeText(rawData.Notes, isMultiline: true); // Não passa TitleCase nas Notas
        var cleanPhone = CleanPhone(rawData.Phone);
        var cleanWhatsApp = CleanPhone(rawData.WhatsApp);

        // Se após a sanitização ficar sem nome... vamos retornar reject e tentar contornar?
        // Normalmente não deve ocorrer rejeição nesse nível, mas deixamos preparado.
        if (string.IsNullOrWhiteSpace(cleanName) && !isDirectoryOrGeneric)
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

        // 2. Validação Rigorosa do E-mail
        if (!string.IsNullOrWhiteSpace(rawData.Email))
        {
            var cleanedEmail = rawData.Email.Trim().ToLowerInvariant();

            // Punição: Se o email tiver lixo concatenado por scraping mal feito (ex: contato@empresa.com.br?subject=xxx ou & ou " ou ' ou espaços brutais).
            var garbageChars = new[] { '?', '&', '"', '\'', '\\', '/', '=', '(', ')' };
            int garbageIndex = cleanedEmail.IndexOfAny(garbageChars);
            if (garbageIndex > 0)
            {
                cleanedEmail = cleanedEmail.Substring(0, garbageIndex);
            }

            if (cleanedEmail.Contains(","))
            {
                cleanedEmail = cleanedEmail.Split(',')[0].Trim();
            }

            if (EmailRegex.IsMatch(cleanedEmail))
            {
                var parts = cleanedEmail.Split('@');
                var prefix = parts[0];
                var domain = parts[1];

                // 3. Detecção de Domínio Suspeito (Agora incluindo sulfixos exóticos)
                bool isSuspiciousDomain = SuspiciousDomains.Contains(domain) ||
                                          SuspiciousDomainSuffixes.Any(ext => domain.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

                if (isSuspiciousDomain)
                {
                    result.HasSuspiciousDomain = true;
                    // Se for bizarro demais, derrubamos.
                    result.IsEmailValid = false;
                    result.Email = null;
                }
                else
                {
                    result.Email = cleanedEmail;
                    result.IsEmailValid = true;

                    // 4. Classificação de Generic Emails
                    if (GenericEmailPrefixes.Contains(prefix))
                    {
                        result.EmailType = EmailType.GenericCorporate;
                    }
                    else
                    {
                        result.EmailType = EmailType.PersonalDirect;
                    }
                }
            }
            else
            {
                result.IsEmailValid = false;
                result.Email = null;
            }
        }

        // Regra de Ouro 8 e 9: Se NÃO tiver e-mail E NÃO tiver telefone válido, rejeita.
        if (!result.IsEmailValid && string.IsNullOrWhiteSpace(result.Phone) && string.IsNullOrWhiteSpace(result.WhatsApp))
        {
            result.ShouldReject = true;
            result.RejectionReason = "Lead lacks any commercial viable contact point (No valid Email, no Phone).";
            return result;
        }

        // 5. Determinação da Qualidade Rígida
        int score = 0;

        if (result.IsEmailValid && result.EmailType == EmailType.PersonalDirect) score += 4;
        if (result.IsEmailValid && result.EmailType == EmailType.GenericCorporate) score += 2;
        if (!string.IsNullOrWhiteSpace(result.Phone) || !string.IsNullOrWhiteSpace(result.WhatsApp)) score += 3;
        if (!string.IsNullOrWhiteSpace(result.CompanyName) && !isDirectoryOrGeneric) score += 3;

        if (isDirectoryOrGeneric) score -= 10;
        if (result.HasSuspiciousDomain) score -= 5;

        if (score >= 8) result.Quality = LeadQuality.High;
        else if (score >= 4) result.Quality = LeadQuality.Medium;
        else result.Quality = LeadQuality.Low;

        // 6. Elegibilidade para Campanhas
        result.IsEligibleForCampaigns =
            result.IsEmailValid &&
            !result.HasSuspiciousDomain &&
            !isDirectoryOrGeneric &&
            result.Quality != LeadQuality.Low;

        return result;
    }

    /// <summary>
    /// Limpa e aplica Title Case numa String para corrigir Caps Locks indesejados e decodes zoados.
    /// Em seguida normaliza, remove spam promocional, trunca e limpa a string final.
    /// </summary>
    private static string SanitizeText(string? input, bool isMultiline = false)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Limpa espaços no inicio e fim e substitui múltiplos espaços internos
        var clean = Regex.Replace(input.Trim(), @"\s+", " ");

        // Corrige encodings rasgados comuns da internet ex: UTF8 lido como ISO que virou ISO salvo de forma errada
        // Executado DEPOIS do Trim/Regex initial para que não tentemos varrer lixo muito profundo.
        clean = FixBrokenEncoding(clean);

        // Se for um bloco multiline, preserva o tamanho e não faz uppercase forçado nem sanitização de nome pesado.
        if (isMultiline)
        {
            return clean;
        }

        // Reduz capitalização abusiva: se 80% do texto está em MAIÚSCULO, a gente abaixa para o titlecase funcionar.
        // O TitleCase do .NET não funciona em frases TODA MAIUSCULAS.
        var upperCount = clean.Count(char.IsUpper);
        if (clean.Length > 0 && ((double)upperCount / clean.Length) > 0.4)
        {
             clean = clean.ToLowerInvariant();
        }

        // Aplica e limpa sujeiras de SEO e slogans (Ex: "Loja de Biquinis - Compre e Arrase")
        clean = NormalizeName(clean);

        // Finalmente, TitleCase para ficar bonito
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        clean = textInfo.ToTitleCase(clean);

        return clean;
    }

    /// <summary>
    /// Aplica as regras 2, 3, 4 e 5: Retira pontuações promocionais, slogans de SEO longos e desduplica palavras.
    /// </summary>
    private static string NormalizeName(string input)
    {
        // Regra 3 e 4: Corta qualquer coisa depois de sentenças promocionais ou separadores abusivos.
        // "Loja de Roupa. Compre agora!" -> "Loja de Roupa"
        // "Minha Loja - As Melhores Roupas" -> "Minha Loja"
        var sentenceSeparators = new[] { '.', '-', '|', '!', '?' };
        var firstSeparatorIndex = input.IndexOfAny(sentenceSeparators);

        if (firstSeparatorIndex > 0)
        {
            // Verificamos se era apenas uma frase normal (sem ser uma sigla como "Dr.")
            var preSeparator = input.Substring(0, firstSeparatorIndex).Trim();
            if (preSeparator.Length > 2)
            {
               input = preSeparator;
            }
        }

        // Filtro limpa-frases promocionais no meio
        foreach (var keyword in PromotionalKeywords)
        {
            if (input.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                input = Regex.Replace(input, Regex.Escape(keyword), "", RegexOptions.IgnoreCase).Trim();
            }
        }

        // Regra de Duplicação de Palavras (Ex: "Loja Teste Maria Loja Teste" -> "Loja Teste Maria")
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var uniqueWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var filteredWords = new List<string>();

        foreach (var word in words)
        {
            // Palavras muito curtas (de, e, da) repetidas são permitidas, substantivos não.
            if (word.Length <= 3 || uniqueWords.Add(word))
            {
                filteredWords.Add(word);
            }
        }
        var noDuplicates = string.Join(" ", filteredWords);

        // Regra 5: Limitar strings ridiculamente longas (Descrições injetadas no nome)
        if (noDuplicates.Length > 80)
        {
            noDuplicates = noDuplicates.Substring(0, 80).Trim();
        }

        return noDuplicates;
    }

    /// <summary>
    /// Tenta consertar caracteres de encoding corrompidos e casos Mojibake do scraping ("EscritRio")
    /// </summary>
    private static string FixBrokenEncoding(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var replacements = new Dictionary<string, string>
        {
            { "Ã¡", "á" }, { "Ã ", "à" }, { "Ã¢", "â" }, { "Ã£", "ã" }, { "Ã¤", "ä" },
            { "Ã©", "é" }, { "Ã¨", "è" }, { "Ãª", "ê" }, { "Ã«", "ë" },
            { "Ã­", "í" }, { "Ã¬", "ì" }, { "Ã®", "î" }, { "Ã¯", "ï" },
            { "Ã³", "ó" }, { "Ã²", "ò" }, { "Ã´", "ô" }, { "Ãµ", "õ" }, { "Ã¶", "ö" },
            { "Ãº", "ú" }, { "Ã¹", "ù" }, { "Ã»", "û" }, { "Ã¼", "ü" },
            { "Ã§", "ç" }, { "Ã±", "ñ" },
            { "Ã?", "Á" }, { "Ã\u0080", "À" }, { "Ã\u0082", "Â" }, { "Ã\u0083", "Ã" }, { "Ã\u0084", "Ä" },
            { "Ã\u0089", "É" }, { "Ã\u0088", "È" }, { "Ã\u008A", "Ê" }, { "Ã\u008B", "Ë" },
            { "Ã\u008D", "Í" }, { "Ã\u008C", "Ì" }, { "Ã\u008E", "Î" }, { "Ã\u008F", "Ï" },
            { "Ã\u0093", "Ó" }, { "Ã\u0092", "Ò" }, { "Ã\u0094", "Ô" }, { "Ã\u0095", "Õ" }, { "Ã\u0096", "Ö" },
            { "Ã\u009A", "Ú" }, { "Ã\u0099", "Ù" }, { "Ã\u009B", "Û" }, { "Ã\u009C", "Ü" },
            { "Ã\u0087", "Ç" }, { "Ã\u0091", "Ñ" },
            { "Âº", "º" }, { "Âª", "ª" }, { "â€“", "–" }, { "&amp;", "&" },
            { "&quot;", "\"" }, { "&lt;", "<" }, { "&gt;", ">" }
        };

        var sb = new StringBuilder(input);
        foreach (var kvp in replacements)
        {
            sb.Replace(kvp.Key, kvp.Value);
        }

        var decoded = sb.ToString();

        // Pass/Regex 2: Conserta Mojibakes PT-BR onde o char perdeu pro Unicode "".
        // Regex busca "" e com base no contexto tenta restaurar (Case-insensitive via Regex mas restaurando capitalização).
        decoded = Regex.Replace(decoded, @"Rio\b", "ório", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"MVeis\b", "Móveis", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"BiquNis\b", "Biquínis", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"MaiS\b", "Maiôs", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"SaDas\b", "Saídas", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"AcessRios\b", "Acessórios", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"VerO\b", "Verão", RegexOptions.IgnoreCase);

        return decoded;
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
