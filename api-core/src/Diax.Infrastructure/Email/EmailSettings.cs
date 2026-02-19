namespace Diax.Infrastructure.Email;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "DIAX CRM";
    public int DispatchIntervalMinutes { get; set; } = 5;
    public int DailyLimit { get; set; } = 250;
    public int HourlyLimit { get; set; } = 50;
    public int BatchSize { get; set; } = 50;
}
