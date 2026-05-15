using System.Text.RegularExpressions;
using Diax.Application.EmailMarketing.Pro.Dtos;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;

namespace Diax.Application.EmailMarketing.Pro;

public class SmartPreselectionService : ISmartPreselectionService
{
    private static readonly Regex EmailRegex =
        new(@"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);

    private static readonly string[] ProviderNames = ["Brevo", "Mailjet", "Resend", "SendGrid", "MailerSend", "ElasticEmail"];

    private readonly ICustomerRepository _customerRepository;
    private readonly INameNormalizationService _nameNormalizer;

    public SmartPreselectionService(
        ICustomerRepository customerRepository,
        INameNormalizationService nameNormalizer)
    {
        _customerRepository = customerRepository;
        _nameNormalizer = nameNormalizer;
    }

    public async Task<SmartPreselectResponse> PreselecAsync(
        SmartPreselectRequest request,
        CancellationToken cancellationToken = default)
    {
        var maxPerProvider = Math.Max(1, request.MaxPerProvider);
        var cooldown = TimeSpan.FromDays(Math.Max(0, request.CooldownDays));
        var now = DateTime.UtcNow;
        var cooldownCutoff = now - cooldown;

        var allLeads = await _customerRepository.GetAllLeadsAsync(cancellationToken);

        var candidates = allLeads
            .Where(c =>
                c.Segment.HasValue &&
                request.Segments.Contains((int)c.Segment.Value) &&
                !string.IsNullOrWhiteSpace(c.Email) &&
                EmailRegex.IsMatch(c.Email) &&
                !c.EmailOptOut &&
                (c.LastEmailSentAt is null || c.LastEmailSentAt < cooldownCutoff) &&
                (c.LeadScore ?? 0) >= request.MinScore)
            .OrderByDescending(c => (int)(c.Segment ?? LeadSegment.Cold))
            .ThenByDescending(c => c.LeadScore ?? 0)
            .ThenBy(c => c.LastEmailSentAt ?? DateTime.MinValue)
            .ToList();

        var warnings = new List<string>();
        if (candidates.Count == 0)
        {
            warnings.Add("Nenhum lead elegível encontrado com os critérios informados.");
            return new SmartPreselectResponse { Warnings = warnings };
        }

        // Distribute evenly across providers
        var totalTarget = maxPerProvider * ProviderNames.Length;
        var selectedLeads = candidates.Take(totalTarget).ToList();

        if (selectedLeads.Count < totalTarget)
        {
            warnings.Add($"Apenas {selectedLeads.Count} lead(s) disponíveis (meta: {totalTarget}).");
        }

        var providerCounts = new Dictionary<string, int>();
        foreach (var name in ProviderNames)
            providerCounts[name] = 0;

        var result = new List<PreselectedLeadDto>();

        for (var i = 0; i < selectedLeads.Count; i++)
        {
            var lead = selectedLeads[i];
            var providerName = ProviderNames[i % ProviderNames.Length];
            providerCounts[providerName]++;

            var firstName = !string.IsNullOrEmpty(lead.NormalizedName)
                ? lead.NormalizedName.Split(' ')[0]
                : _nameNormalizer.NormalizeName(lead.Name) ?? _nameNormalizer.NormalizeName(lead.CompanyName);
            if (string.IsNullOrEmpty(firstName))
                firstName = lead.Name.Split(' ')[0];

            var segLabel = lead.Segment switch
            {
                LeadSegment.Hot  => "Hot",
                LeadSegment.Warm => "Warm",
                _                => "Cold"
            };

            var daysSinceEmail = lead.LastEmailSentAt.HasValue
                ? (int)(now - lead.LastEmailSentAt.Value).TotalDays
                : (int?)null;

            var reason = daysSinceEmail.HasValue
                ? $"{segLabel}, score {lead.LeadScore ?? 0}, último email há {daysSinceEmail}d"
                : $"{segLabel}, score {lead.LeadScore ?? 0}, nunca contatado";

            result.Add(new PreselectedLeadDto
            {
                CustomerId         = lead.Id,
                Name               = lead.Name,
                FirstName          = firstName,
                Email              = lead.Email!,
                AssignedProvider   = providerName,
                Segment            = (int)(lead.Segment ?? LeadSegment.Cold),
                SegmentLabel       = segLabel,
                Score              = lead.LeadScore ?? 0,
                ReasonForSelection = reason,
                LastEmailSentAt    = lead.LastEmailSentAt,
            });
        }

        return new SmartPreselectResponse
        {
            Leads          = result,
            TotalSelected  = result.Count,
            ProviderCounts = providerCounts,
            Warnings       = warnings,
        };
    }
}
