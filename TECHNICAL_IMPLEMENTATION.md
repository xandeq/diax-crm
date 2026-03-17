# 🔧 Guia Técnico de Implementação

## Proposta #1: Email Subject Line Optimizer

### Backend Architecture

**New Service: `EmailSubjectOptimizerService.cs`**

```csharp
namespace Diax.Application.AI.EmailOptimization;

public interface IEmailSubjectOptimizerService
{
    Task<GenerateSubjectLinesResponse> GenerateSubjectLinesAsync(
        GenerateSubjectLinesRequest request, string userId);
}

public class EmailSubjectOptimizerService : IEmailSubjectOptimizerService
{
    private readonly IPromptGeneratorService _promptService;
    private readonly IHumanizeTextService _humanizeService;

    public async Task<GenerateSubjectLinesResponse> GenerateSubjectLinesAsync(
        GenerateSubjectLinesRequest request, string userId)
    {
        // 1. Reusa PromptGeneratorService com framework customizado
        var prompt = $@"
Gere 5 subject lines otimizadas para: {request.BaseMessage}
Audiência: {request.TargetAudience}
Máximo 50 caracteres cada.
Retorne como JSON com campos: text, angle, estimatedOpenRate
";

        var generatedPrompt = await _promptService.GeneratePromptAsync(
            new GeneratePromptRequest
            {
                RawInput = prompt,
                PromptType = PromptType.Professional,
                Provider = request.Provider,
                Model = request.Model
            },
            userId
        );

        // 2. Executar gegen LLM e parsear resposta
        // ... implementação completa ...

        return new GenerateSubjectLinesResponse
        {
            SubjectLines = result.Subjects.Select(s => new SubjectLineDto
            {
                Text = s.Text,
                Angle = s.Angle,
                EstimatedOpenRate = s.EstimatedOpenRate
            }).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }
}
```

### Frontend Component

**Page: `/utilities/email-subject-optimizer`**

```typescript
export default function EmailSubjectOptimizerPage() {
  const { providers, selectedProvider, selectedModel } = useAiCatalog();
  const [baseMessage, setBaseMessage] = useState('');
  const [generatedSubjects, setGeneratedSubjects] = useState<SubjectLineDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const handleGenerate = async () => {
    setIsLoading(true);
    try {
      const result = await generateEmailSubjectLines({
        baseMessage,
        provider: selectedProvider,
        model: selectedModel || undefined
      });
      setGeneratedSubjects(result.subjectLines);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-8">
      {/* Configuração + Input */}
      <Card>
        <ProviderModelSelector {...} />
        <textarea {...baseMessage} />
        <Button onClick={handleGenerate} disabled={isLoading}>
          Gerar Subject Lines
        </Button>
      </Card>

      {/* Output: 5 Subject Lines em Cards */}
      <div className="grid gap-4">
        {generatedSubjects.map((subject) => (
          <Card key={subject.text} className="cursor-pointer hover:ring-2">
            <CardContent className="p-4">
              <p className="font-medium">{subject.text}</p>
              <p className="text-sm text-muted-foreground">{subject.angle}</p>
              <p className="text-lg font-bold">{(subject.estimatedOpenRate * 100).toFixed(0)}% Open Rate</p>
              <Button variant="outline" size="sm" className="mt-2">Copiar</Button>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
```

### Key Reuse Points

| Component | Reuse Level | Location |
|-----------|-------------|----------|
| `PromptGeneratorService` | 80% | Geração de prompts otimizados |
| `useAiCatalog` hook | 100% | Seleção provider/model |
| `EmailCampaignsController` | 90% | Extrair contexto de campanhas prévias |
| UI pattern Cards | 80% | Cards com preview + copy button |
| `ExecuteAiActionAsync` | 100% | Executar AI action com auth/error handling |

### Database Entity

```csharp
public class EmailOptimization : AuditableEntity
{
    public long Id { get; set; }
    public string UserId { get; set; }
    public long? CampaignId { get; set; }
    public string OriginalSubject { get; set; }
    public List<string> GeneratedSubjects { get; set; }
    public string SelectedSubject { get; set; }
    public decimal? EstimatedOpenRateImprovement { get; set; }
    public bool? ActualOpenRateImproved { get; set; }
}
```

### Estimated Effort

- Backend: 8-10 hours
- Frontend: 4-6 hours
- Tests: 2-3 hours
- **Total: 14-19 hours (≈ 2 days)**

---

## Proposta #2-5: Padrão Análogo

Cada proposta segue o mesmo padrão:

1. **Criar novo Service** (reutilizando PromptGeneratorService 70-85%)
2. **Criar novo Controller** (estendendo BaseApiController)
3. **Criar nova Página** (reutilizando UI patterns)
4. **Criar DTOs + Entity** (mapping automático)
5. **Adicionar migração** (add-migration.ps1)
6. **Registrar no DI** (Program.cs)

### Effort Summary

| Proposta | Backend | Frontend | Tests | Total |
|----------|---------|----------|-------|-------|
| #1 Email Subject | 8-10h | 4-6h | 2-3h | 14-19h |
| #2 Lead Persona | 10-12h | 5-7h | 3-4h | 18-23h |
| #3 A/B Tester | 8-10h | 4-6h | 2-3h | 14-19h |
| #4 Social Batch | 12-15h | 6-8h | 3-4h | 21-27h |
| #5 Insights | 10-12h | 5-7h | 2-3h | 17-22h |
| **TOTAL** | **48-59h** | **24-34h** | **12-17h** | **84-110h** |

**In developer days (8h/day)**: 10-14 days for 1 senior dev, or 5-7 days with 2 devs

---

## Integration Checklist

### For Each Proposal:
- [ ] Create service interface + implementation
- [ ] Create controller with POST/GET endpoints
- [ ] Create DTOs (request/response)
- [ ] Create entity + add EF Core mapping
- [ ] Create migration: `add-migration.ps1 Add{FeatureName}`
- [ ] Register service in `Program.cs`
- [ ] Test endpoint in Swagger
- [ ] Create frontend page
- [ ] Create service client (apiFetch wrapper)
- [ ] Integrate with `useAiCatalog` hook
- [ ] Add to Header navigation
- [ ] Write unit tests (Services, Controllers)
- [ ] Write E2E tests
- [ ] Performance test (LLM latency)
- [ ] Deploy via `deploy-api-core-smarterasp.yml` + `deploy-crm-web-hostgator.yml`

---

## Reusable Patterns

### Pattern 1: AI Service Base Class

```csharp
public abstract class AiOptimizationServiceBase
{
    protected async Task<string> ExtractSuccessContextAsync(
        IEnumerable<dynamic> recentItems, string metric)
    {
        var topPerformers = recentItems
            .OrderByDescending(x => (decimal)x.GetProperty(metric))
            .Take(5);

        return string.Join("\n", topPerformers.Select(item =>
            $"- {item.GetProperty("title")}: {item.GetProperty(metric)}%"));
    }
}
```

### Pattern 2: Reusable Frontend Hook

```typescript
export function useAiOptimization<TRequest, TResponse>(
  apiFn: (request: TRequest) => Promise<TResponse>
) {
  const { selectedProvider, selectedModel } = useAiCatalog();
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<TResponse | null>(null);

  const generate = useCallback(async (request: TRequest) => {
    setIsLoading(true);
    try {
      const data = await apiFn({...request, provider: selectedProvider, model: selectedModel});
      setResult(data);
    } finally {
      setIsLoading(false);
    }
  }, [apiFn, selectedProvider, selectedModel]);

  return { generate, isLoading, result };
}
```

---

## Key Services to Reuse

| Service | Reuse Rate | Key Methods |
|---------|-----------|------------|
| `IPromptGeneratorService` | 70-85% | `GeneratePromptAsync()` |
| `IHumanizeTextService` | 40-85% | `HumanizeAsync(text, tone)` |
| `IAiCatalogService` | 100% | `GetProvidersAsync()`, `GetModelsAsync()` |
| `IEmailCampaignService` | 70-90% | `GetRecentCampaignsAsync()` |
| `ICustomerService` | 70-90% | `GetLeadsAsync()`, `AnalyzeAsync()` |
| `IImageGenerationService` | 80% | `GenerateAsync()` (only for #4) |

---

**Next Step**: Create tickets for each proposta with this specification.
