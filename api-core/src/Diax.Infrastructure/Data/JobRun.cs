namespace Diax.Infrastructure.Data;

/// <summary>
/// Registro "job diário X já rodou no dia Y". Os workers guardavam isso só em memória:
/// cada recycle/deploy depois do horário rodava de novo (briefing duplicado no Telegram).
/// </summary>
public class JobRun
{
    public string Job { get; private set; } = string.Empty;
    public DateOnly RunDate { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }

    private JobRun() { }

    public JobRun(string job, DateOnly runDate)
    {
        Job = job;
        RunDate = runDate;
        CompletedAtUtc = DateTime.UtcNow;
    }
}
