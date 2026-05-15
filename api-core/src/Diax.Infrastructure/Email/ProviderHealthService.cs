using Diax.Application.EmailMarketing.Pro;
using Diax.Application.EmailMarketing.Pro.Dtos;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Microsoft.Extensions.Options;

namespace Diax.Infrastructure.Email;

public class ProviderHealthService : IProviderHealthService
{
    private readonly IEmailQueueRepository _repository;
    private readonly EmailSettings _settings;

    private static readonly (EmailProvider Provider, string Name)[] Providers =
    [
        (EmailProvider.Brevo,        "Brevo"),
        (EmailProvider.Mailjet,      "Mailjet"),
        (EmailProvider.Resend,       "Resend"),
        (EmailProvider.ElasticEmail, "ElasticEmail"),
        (EmailProvider.MailerSend,   "MailerSend"),
        (EmailProvider.SendGrid,     "SendGrid"),
    ];

    public ProviderHealthService(IEmailQueueRepository repository, IOptions<EmailSettings> settings)
    {
        _repository = repository;
        _settings = settings.Value;
    }

    public async Task<List<ProviderHealthDto>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfDay  = now.Date;
        var startOfHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        var dailyLimit  = _settings.DailyLimit  > 0 ? _settings.DailyLimit  : 600;
        var hourlyLimit = _settings.HourlyLimit > 0 ? _settings.HourlyLimit : 150;

        // Per-provider limits approximate daily/3 and hourly/3
        var perProviderDailyLimit  = (int)Math.Ceiling((double)dailyLimit  / Providers.Length);
        var perProviderHourlyLimit = (int)Math.Ceiling((double)hourlyLimit / Providers.Length);

        var result = new List<ProviderHealthDto>();

        foreach (var (provider, name) in Providers)
        {
            var sentToday    = await _repository.CountSentByProviderSinceAsync(provider, startOfDay,  cancellationToken);
            var sentThisHour = await _repository.CountSentByProviderSinceAsync(provider, startOfHour, cancellationToken);
            var queuedCount  = await _repository.CountQueuedByProviderAsync(provider, cancellationToken);
            var failedToday  = await _repository.CountFailedByProviderSinceAsync(provider, startOfDay, cancellationToken);

            var dailyRemaining  = Math.Max(0, perProviderDailyLimit  - sentToday);
            var hourlyRemaining = Math.Max(0, perProviderHourlyLimit - sentThisHour);
            var dailyUsage = perProviderDailyLimit > 0
                ? Math.Round(sentToday * 100.0 / perProviderDailyLimit, 1)
                : 0.0;

            var health = dailyRemaining == 0 || hourlyRemaining == 0
                ? "down"
                : dailyUsage >= 80.0
                    ? "degraded"
                    : "ok";

            result.Add(new ProviderHealthDto
            {
                Provider          = name,
                SentToday         = sentToday,
                DailyLimit        = perProviderDailyLimit,
                DailyRemaining    = dailyRemaining,
                DailyUsagePercent = dailyUsage,
                SentThisHour      = sentThisHour,
                HourlyLimit       = perProviderHourlyLimit,
                HourlyRemaining   = hourlyRemaining,
                QueuedCount       = queuedCount,
                FailedToday       = failedToday,
                Health            = health,
            });
        }

        return result;
    }
}
