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

    private static readonly HashSet<string> PromotionalKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Compre Agora", "Frete GrÃ¡tis", "Arrase no VerÃ£o", "Desconto", "PromoÃ§Ã£o", "Promo", "Oferta",
        "PeÃ§a JÃ¡", "Entrega RÃ¡pida", "Mais Vendido", "Clique Aqui"
    };

    private static readonly HashSet<string> SuspiciousDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com", "10minutemail.com", "tempmail.com", "yopmail.com", "guerrillamail.com",
        "temp-mail.org", "throwawaymail.com", "fakeinbox.com", "dropmail.me", "fakemail.net", "getnada.com"
    };

    private static readonly HashSet<string> SuspiciousDomainSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xyz", ".top", ".club", ".space", ".online", ".site", ".ru", ".tk"
    };

    private static readonly HashSet<string> ForeignCountryTLDs = new(StringComparer.OrdinalIgnoreCase)
    {
        ".es", ".de", ".co", ".ar", ".mx", ".cl", ".pe", ".it", ".fr", ".uk",
        ".jp", ".cn", ".kr", ".in", ".za", ".ng", ".ke", ".pl", ".nl", ".be",
        ".se", ".no", ".dk", ".fi", ".at", ".ch", ".ie", ".nz", ".au",
        ".ve", ".ec", ".bo", ".py", ".uy", ".cz", ".hu", ".ro", ".bg",
        ".hr", ".sk", ".lt", ".lv", ".ee", ".gr", ".tr", ".il", ".ae",
        ".sa", ".ph", ".th", ".vn", ".id", ".my", ".sg", ".tw", ".hk"
    };

    private static readonly HashSet<string> GenericCompanyKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Linktree", "404", "Not Found", "GuiaMais", "Telelistas", "Home", "PÃ¡gina Inicial", "DiretÃ³rio", "Busca", "Erro 404",
        "Default", "Sem Nome", "Site", "Contato",
        "Instagram", "Facebook", "LinkedIn", "Twitter", "TikTok", "YouTube", "Google", "WhatsApp", "Telegram", "Pinterest", "Reddit",
        "Jusbrasil", "Reclame Aqui", "Mercado Livre", "OLX", "iFood", "Shopee", "Magazine Luiza", "Magalu",
        "Americanas", "Casas Bahia", "Submarino", "BuscapÃ©", "Bondfaro", "Zoom", "PagSeguro", "PicPay",
        "GetNinjas", "Habitissimo", "Elo7", "Enjoei", "QuintoAndar", "Viva Real", "Zap ImÃ³veis",
        "iCarros", "WebMotors", "Catho", "Infojobs", "Doctoralia", "Glassdoor", "Indeed",
        "Amazon", "AliExpress", "Wish", "Yelp", "TripAdvisor", "Booking", "Trivago",
        "Wikipedia", "WikiHow", "Stack Overflow", "Github", "ZocDoc"
    };

    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z]{2,})+$",
        RegexOptions.CultureInvariant);

    private static readonly Regex SearchPhraseEnding = new(
        @"\s+(em|no|na|nos|nas|de|do|da|dos|das|para)\s+[A-Za-zÀ-ÖØ-öø-ÿ]+(\s+[A-Za-zÀ-ÖØ-öø-ÿ]+)*\s*$",
        RegexOptions.IgnoreCase);

    private static readonly HashSet<string> SearchCategoryWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "hospitais", "clÃ­nicas", "clinicas", "restaurantes", "hotÃ©is", "hoteis", "lojas", "escritÃ³rios", "escritorios",
        "farmÃ¡cias", "farmacias", "escolas", "academias", "consultÃ³rios", "consultorios", "laboratÃ³rios", "laboratorios",
        "mercados", "supermercados", "padarias", "bares", "lanchonetes", "pizzarias", "postos", "oficinas",
        "dentistas", "mÃ©dicos", "medicos", "advogados", "contadores", "engenheiros", "veterinÃ¡rios", "veterinarios",
        "imobiliÃ¡rias", "imobiliarias", "construtoras", "transportadoras", "distribuidoras", "fÃ¡bricas", "fabricas",
        "empresas", "serviÃ§os", "servicos", "profissionais", "especialistas", "fornecedores", "clÃ­nica", "clinica"
    };

    public SanitizedLeadResult SanitizeAndClassify(RawLeadData rawData)
    {
        var result = new SanitizedLeadResult();

        var cleanName = SanitizeText(rawData.Name);
        var cleanCompany = SanitizeText(rawData.CompanyName);
        var cleanNotes = SanitizeText(rawData.Notes, isMultiline: true);
        var cleanPhone = CleanPhone(rawData.Phone);
        var cleanWhatsApp = CleanPhone(rawData.WhatsApp);

        var resolvedBusinessName = ResolveBusinessName(cleanCompany, cleanName);
        var businessCandidates = new[] { cleanCompany, cleanName }
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        bool isDirectoryOrGeneric = false;
        if (!string.IsNullOrWhiteSpace(resolvedBusinessName))
        {
            cleanName = resolvedBusinessName;
            if (string.IsNullOrWhiteSpace(cleanCompany))
            {
                cleanCompany = resolvedBusinessName;
            }
        }
        else if (businessCandidates.Any(LooksLikeDirectoryOrGenericProfile))
        {
            isDirectoryOrGeneric = true;
            result.ShouldReject = true;
            result.RejectionReason = "Lead originates from a Generic Directory/Profile (e.g., Linktree, GuiaMais) rather than real business context.";
        }
        else if (businessCandidates.Any(IsSearchPhrase))
        {
            isDirectoryOrGeneric = true;
            result.ShouldReject = true;
            result.RejectionReason = "Name appears to be a search phrase, not a business name.";
        }

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

        if (!string.IsNullOrWhiteSpace(rawData.Email))
        {
            var cleanedEmail = rawData.Email.Trim().ToLowerInvariant();

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

                bool isSuspiciousDomain = SuspiciousDomains.Contains(domain) ||
                                          SuspiciousDomainSuffixes.Any(ext => domain.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

                if (!isSuspiciousDomain)
                {
                    var tld = "." + domain.Split('.').Last();
                    bool isCompositeDomain = domain.Contains(".com.") || domain.Contains(".co.") ||
                                             domain.Contains(".org.") || domain.Contains(".net.") ||
                                             domain.Contains(".gov.") || domain.Contains(".edu.");
                    if (!isCompositeDomain && ForeignCountryTLDs.Contains(tld))
                    {
                        isSuspiciousDomain = true;
                    }
                }

                if (isSuspiciousDomain)
                {
                    result.HasSuspiciousDomain = true;
                    result.IsEmailValid = false;
                    result.Email = null;
                }
                else
                {
                    result.Email = cleanedEmail;
                    result.IsEmailValid = true;
                    result.EmailType = GenericEmailPrefixes.Contains(prefix)
                        ? EmailType.GenericCorporate
                        : EmailType.PersonalDirect;
                }
            }
            else
            {
                result.IsEmailValid = false;
                result.Email = null;
            }
        }

        if (!result.IsEmailValid && string.IsNullOrWhiteSpace(result.Phone) && string.IsNullOrWhiteSpace(result.WhatsApp))
        {
            result.ShouldReject = true;
            result.RejectionReason = "Lead lacks any commercial viable contact point (No valid Email, no Phone).";
            return result;
        }

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

        result.IsEligibleForCampaigns =
            result.IsEmailValid &&
            !result.HasSuspiciousDomain &&
            !isDirectoryOrGeneric &&
            result.Quality != LeadQuality.Low;

        return result;
    }

    private static string SanitizeText(string? input, bool isMultiline = false)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var clean = Regex.Replace(input.Trim(), @"\s+", " ");
        clean = FixBrokenEncoding(clean);

        if (isMultiline)
        {
            return clean;
        }

        var upperCount = clean.Count(char.IsUpper);
        if (clean.Length > 0 && ((double)upperCount / clean.Length) > 0.4)
        {
            clean = clean.ToLowerInvariant();
        }

        clean = NormalizeName(clean);

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        clean = textInfo.ToTitleCase(clean);

        return clean;
    }

    private static string NormalizeName(string input)
    {
        var sentenceSeparators = new[] { '.', '-', '|', '!', '?' };
        var firstSeparatorIndex = input.IndexOfAny(sentenceSeparators);

        if (firstSeparatorIndex > 0)
        {
            var preSeparator = input.Substring(0, firstSeparatorIndex).Trim();
            if (preSeparator.Length > 2)
            {
                input = preSeparator;
            }
        }

        foreach (var keyword in PromotionalKeywords)
        {
            if (input.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                input = Regex.Replace(input, Regex.Escape(keyword), "", RegexOptions.IgnoreCase).Trim();
            }
        }

        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var uniqueWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var filteredWords = new List<string>();

        foreach (var word in words)
        {
            if (word.Length <= 3 || uniqueWords.Add(word))
            {
                filteredWords.Add(word);
            }
        }

        var noDuplicates = string.Join(" ", filteredWords);
        if (noDuplicates.Length > 80)
        {
            noDuplicates = noDuplicates.Substring(0, 80).Trim();
        }

        return noDuplicates;
    }

    private static string? ResolveBusinessName(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (LooksLikeDirectoryOrGenericProfile(candidate) || IsSearchPhrase(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static bool LooksLikeDirectoryOrGenericProfile(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalizedInput = NormalizeForComparison(input);
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return false;
        }

        foreach (var keyword in GenericCompanyKeywords)
        {
            var normalizedKeyword = NormalizeForComparison(keyword);
            if (normalizedInput == normalizedKeyword)
            {
                return true;
            }

            if (normalizedInput.StartsWith(normalizedKeyword + " ", StringComparison.Ordinal) ||
                normalizedInput.StartsWith(normalizedKeyword + "-", StringComparison.Ordinal) ||
                normalizedInput.StartsWith(normalizedKeyword + "|", StringComparison.Ordinal) ||
                normalizedInput.StartsWith(normalizedKeyword + ":", StringComparison.Ordinal) ||
                normalizedInput.EndsWith(" " + normalizedKeyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeForComparison(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) || ch is '-' or '|' or ':')
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    private static string FixBrokenEncoding(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        input = input.Replace("\uFFFD", "");
        input = Regex.Replace(input, @"\s{2,}", " ").Trim();

        if (string.IsNullOrWhiteSpace(input)) return input;

        var replacements = new Dictionary<string, string>
        {
            { "ÃƒÂ¡", "Ã¡" }, { "ÃƒÂ ", "Ã " }, { "ÃƒÂ¢", "Ã¢" }, { "ÃƒÂ£", "Ã£" }, { "ÃƒÂ¤", "Ã¤" },
            { "ÃƒÂ©", "Ã©" }, { "ÃƒÂ¨", "Ã¨" }, { "ÃƒÂª", "Ãª" }, { "ÃƒÂ«", "Ã«" },
            { "ÃƒÂ­", "Ã­" }, { "ÃƒÂ¬", "Ã¬" }, { "ÃƒÂ®", "Ã®" }, { "ÃƒÂ¯", "Ã¯" },
            { "ÃƒÂ³", "Ã³" }, { "ÃƒÂ²", "Ã²" }, { "ÃƒÂ´", "Ã´" }, { "ÃƒÂµ", "Ãµ" }, { "ÃƒÂ¶", "Ã¶" },
            { "ÃƒÂº", "Ãº" }, { "ÃƒÂ¹", "Ã¹" }, { "ÃƒÂ»", "Ã»" }, { "ÃƒÂ¼", "Ã¼" },
            { "ÃƒÂ§", "Ã§" }, { "ÃƒÂ±", "Ã±" },
            { "Ãƒ?", "Ã" }, { "Ãƒ\u0080", "Ã€" }, { "Ãƒ\u0082", "Ã‚" }, { "Ãƒ\u0083", "Ãƒ" }, { "Ãƒ\u0084", "Ã„" },
            { "Ãƒ\u0089", "Ã‰" }, { "Ãƒ\u0088", "Ãˆ" }, { "Ãƒ\u008A", "ÃŠ" }, { "Ãƒ\u008B", "Ã‹" },
            { "Ãƒ\u008D", "Ã" }, { "Ãƒ\u008C", "ÃŒ" }, { "Ãƒ\u008E", "ÃŽ" }, { "Ãƒ\u008F", "Ã" },
            { "Ãƒ\u0093", "Ã“" }, { "Ãƒ\u0092", "Ã’" }, { "Ãƒ\u0094", "Ã”" }, { "Ãƒ\u0095", "Ã•" }, { "Ãƒ\u0096", "Ã–" },
            { "Ãƒ\u009A", "Ãš" }, { "Ãƒ\u0099", "Ã™" }, { "Ãƒ\u009B", "Ã›" }, { "Ãƒ\u009C", "Ãœ" },
            { "Ãƒ\u0087", "Ã‡" }, { "Ãƒ\u0091", "Ã‘" },
            { "Ã‚Âº", "Âº" }, { "Ã‚Âª", "Âª" }, { "Ã¢â‚¬â€œ", "â€“" }, { "&amp;", "&" },
            { "&quot;", "\"" }, { "&lt;", "<" }, { "&gt;", ">" }
        };

        var sb = new StringBuilder(input);
        foreach (var kvp in replacements)
        {
            sb.Replace(kvp.Key, kvp.Value);
        }

        var decoded = sb.ToString();
        decoded = CollapseRepeatedChars(decoded);

        decoded = Regex.Replace(decoded, @"Rio\b", "Ã³rio", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"MVeis\b", "MÃ³veis", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"BiquNis\b", "BiquÃ­nis", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"MaiS\b", "MaiÃ´s", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"SaDas\b", "SaÃ­das", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"AcessRios\b", "AcessÃ³rios", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"VerO\b", "VerÃ£o", RegexOptions.IgnoreCase);

        decoded = Regex.Replace(decoded, @"\bSade\b", "SaÃºde", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bEstdio\b", "EstÃºdio", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bMdico\b", "MÃ©dico", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bMdica\b", "MÃ©dica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bJrdico\b", "JurÃ­dico", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bJrdica\b", "JurÃ­dica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bNegcios\b", "NegÃ³cios", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bComrcio\b", "ComÃ©rcio", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bFarmcia\b", "FarmÃ¡cia", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bMecnica\b", "MecÃ¢nica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bClnica\b", "ClÃ­nica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bPtio\b", "PÃ¡tio", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bRstico\b", "RÃºstico", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bRstica\b", "RÃºstica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bAcadmia\b", "Academia", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bEdifcio\b", "EdifÃ­cio", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bEletrnica\b", "EletrÃ´nica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bTcnico\b", "TÃ©cnico", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bTcnica\b", "TÃ©cnica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bPblica\b", "PÃºblica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bPblico\b", "PÃºblico", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bPrtica\b", "PrÃ¡tica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bLgica\b", "LÃ³gica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bOdontolgica\b", "OdontolÃ³gica", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bOdontolgico\b", "OdontolÃ³gico", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bVeterinria\b", "VeterinÃ¡ria", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bVeterinrio\b", "VeterinÃ¡rio", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bImveis\b", "ImÃ³veis", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bServios\b", "ServiÃ§os", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bSolues\b", "SoluÃ§Ãµes", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bEducao\b", "EducaÃ§Ã£o", RegexOptions.IgnoreCase);
        decoded = Regex.Replace(decoded, @"\bInformtica\b", "InformÃ¡tica", RegexOptions.IgnoreCase);

        return decoded;
    }

    private static string CollapseRepeatedChars(string input)
    {
        if (input.Length < 3) return input;

        var sb = new StringBuilder(input.Length);
        sb.Append(input[0]);
        int repeatCount = 1;

        for (int i = 1; i < input.Length; i++)
        {
            if (char.ToLowerInvariant(input[i]) == char.ToLowerInvariant(input[i - 1]))
            {
                repeatCount++;
                if (repeatCount <= 2)
                {
                    sb.Append(input[i]);
                }
            }
            else
            {
                repeatCount = 1;
                sb.Append(input[i]);
            }
        }

        return sb.ToString();
    }

    private static bool IsSearchPhrase(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var trimmed = name.Trim();

        if (SearchPhraseEnding.IsMatch(trimmed))
        {
            var firstWord = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (firstWord != null && SearchCategoryWords.Contains(firstWord))
                return true;

            if (trimmed.Contains(" E ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split(new[] { " E ", " e " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var firstPart = parts[0].Trim().Split(' ').FirstOrDefault();
                    if (firstPart != null && SearchCategoryWords.Contains(firstPart))
                        return true;
                }
            }
        }

        var wordsArr = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (wordsArr.Length >= 3 && wordsArr.Length <= 6)
        {
            var eIndex = Array.FindIndex(wordsArr, w => w.Equals("E", StringComparison.OrdinalIgnoreCase));
            if (eIndex > 0 && eIndex < wordsArr.Length - 1)
            {
                if (SearchCategoryWords.Contains(wordsArr[0]) && SearchCategoryWords.Contains(wordsArr[eIndex + 1]))
                    return true;
            }
        }

        return false;
    }

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
