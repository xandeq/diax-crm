namespace Diax.Application.AI.LeadPersona;

public class GeneratePersonasResponseDto
{
    public PersonaDto[] Personas { get; set; } = [];
    public string RequestId { get; set; } = null!;
    public string? ProviderUsed { get; set; }
    public string? ModelUsed { get; set; }
    public double CompletionTime { get; set; } // in milliseconds
    public int LeadsAnalyzed { get; set; }
}

public class PersonaDto
{
    public int Id { get; set; } // Persona number (1-5)
    public string Name { get; set; } = null!; // Persona name (e.g., "Carlos, Tech Director")
    public string Title { get; set; } = null!; // Job title
    public string CompanyType { get; set; } = null!; // Company size/type (e.g., "Tech Startup", "Mid-size Corp")
    public string Industry { get; set; } = null!; // Industry focus
    public string[] PainPoints { get; set; } = []; // 2-3 main pain points
    public string[] Goals { get; set; } = []; // What they're trying to achieve
    public string BudgetRange { get; set; } = null!; // e.g., "$5k-$25k/year"
    public string DecisionProcess { get; set; } = null!; // How they make buying decisions
    public string[] EffectiveChannels { get; set; } = []; // Best ways to reach them
    public string[] OutreachMessages { get; set; } = []; // 2-3 tailored message angles
    public string[] LeadExamples { get; set; } = []; // Examples from the analyzed leads
    public int PercentageOfLeads { get; set; } // What % of your leads match this persona
}
