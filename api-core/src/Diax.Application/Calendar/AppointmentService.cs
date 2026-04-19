using System.Text;
using Diax.Application.Calendar.Dtos;
using Diax.Domain.Auth;
using Diax.Domain.Calendar;
using Diax.Domain.Common;
using Diax.Domain.EmailMarketing;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Calendar;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AppointmentService> _logger;
    private readonly Diax.Application.PromptGenerator.IPromptGeneratorService _promptGenerator;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IEmailQueueRepository emailQueueRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<AppointmentService> logger,
        Diax.Application.PromptGenerator.IPromptGeneratorService promptGenerator)
    {
        _appointmentRepository = appointmentRepository;
        _emailQueueRepository = emailQueueRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
        _promptGenerator = promptGenerator;
    }

    public async Task<Result<AppointmentDto>> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Result.Failure<AppointmentDto>(new Error("Unauthorized", "User is not authenticated."));

        var appointment = new Appointment
        {
            Title = dto.Title,
            Description = dto.Description,
            Date = dto.Date,
            Type = dto.Type,
            DurationMinutes = dto.DurationMinutes > 0 ? dto.DurationMinutes : 60,
            LabelId = dto.LabelId,
            UserId = userId.Value
        };

        await _appointmentRepository.AddAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(appointment));
    }

    public async Task<Result<AppointmentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdWithLabelAsync(id, cancellationToken);

        if (appointment == null)
            return Result.Failure<AppointmentDto>(new Error("NotFound", "Appointment not found."));

        return Result.Success(MapToDto(appointment));
    }

    public async Task<Result<IEnumerable<AppointmentDto>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var appointments = await _appointmentRepository.GetByDateRangeWithLabelAsync(startDate, endDate, cancellationToken);
        return Result.Success(appointments.Select(MapToDto));
    }

    public async Task<Result<AppointmentDto>> UpdateAsync(Guid id, UpdateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdWithLabelAsync(id, cancellationToken);

        if (appointment == null)
            return Result.Failure<AppointmentDto>(new Error("NotFound", "Appointment not found."));

        if (dto.Title != null) appointment.Title = dto.Title;
        if (dto.Description != null) appointment.Description = dto.Description;
        if (dto.Date.HasValue) appointment.Date = dto.Date.Value;
        if (dto.Type.HasValue) appointment.Type = dto.Type.Value;
        if (dto.DurationMinutes.HasValue && dto.DurationMinutes.Value > 0) appointment.DurationMinutes = dto.DurationMinutes.Value;
        if (dto.LabelId.HasValue) appointment.LabelId = dto.LabelId;

        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(appointment));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken);

        if (appointment == null)
            return Result.Failure(new Error("NotFound", "Appointment not found."));

        await _appointmentRepository.DeleteAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteWithScopeAsync(Guid id, string scope, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken);
        if (appointment == null)
            return Result.Failure(new Error("NotFound", "Appointment not found."));

        if (scope == "one" || appointment.RecurrenceGroupId == null)
        {
            // Soft-cancel apenas esta ocorrência
            appointment.IsCancelled = true;
            await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
        }
        else if (scope == "forward" && appointment.RecurrenceGroupId.HasValue)
        {
            // Cancela este e os seguintes da série
            var series = await _appointmentRepository.GetByRecurrenceGroupAsync(
                appointment.RecurrenceGroupId.Value, cancellationToken);
            foreach (var a in series.Where(a => a.Date >= appointment.Date))
            {
                a.IsCancelled = true;
                await _appointmentRepository.UpdateAsync(a, cancellationToken);
            }
        }
        else if (scope == "all" && appointment.RecurrenceGroupId.HasValue)
        {
            // Deleta toda a série
            var series = await _appointmentRepository.GetByRecurrenceGroupAsync(
                appointment.RecurrenceGroupId.Value, cancellationToken);
            foreach (var a in series)
                await _appointmentRepository.DeleteAsync(a, cancellationToken);
        }
        else
        {
            await _appointmentRepository.DeleteAsync(appointment, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IEnumerable<AppointmentDto>>> CreateRecurringAsync(RecurringAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Result.Failure<IEnumerable<AppointmentDto>>(new Error("Unauthorized", "User is not authenticated."));

        if (!TimeOnly.TryParse(dto.TimeHHmm, out var time))
            return Result.Failure<IEnumerable<AppointmentDto>>(new Error("Validation", "Invalid time format. Use HH:mm."));

        var groupId = Guid.NewGuid();
        var excludedSet = new HashSet<string>(dto.ExcludedDates ?? []);
        var created = new List<Appointment>();

        var current = dto.StartDate;
        while (current <= dto.EndDate)
        {
            var dayOfWeek = (int)current.DayOfWeek;
            var dateStr = current.ToString("yyyy-MM-dd");

            if (dto.DaysOfWeek.Contains(dayOfWeek) && !excludedSet.Contains(dateStr))
            {
                // Combina data + hora como UTC-3 (Brasília) → UTC
                var localDt = new DateTime(current.Year, current.Month, current.Day,
                    time.Hour, time.Minute, 0, DateTimeKind.Unspecified);
                var utcDt = TimeZoneInfo.ConvertTimeToUtc(localDt,
                    TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));

                var appt = new Appointment
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Type = dto.Type,
                    Date = utcDt,
                    DurationMinutes = dto.DurationMinutes > 0 ? dto.DurationMinutes : 60,
                    LabelId = dto.LabelId,
                    RecurrenceGroupId = groupId,
                    UserId = userId.Value
                };
                created.Add(appt);
                await _appointmentRepository.AddAsync(appt, cancellationToken);
            }

            current = current.AddDays(1);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(created.Select(MapToDto));
    }

    public async Task<Result> SendDailyAgendaNotificationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando envio de notificações diárias de agenda...");

        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);

        var pendingAppointments = await _appointmentRepository.GetPendingDailyNotificationAsync(todayStart, todayEnd, cancellationToken);
        var pendingList = pendingAppointments.ToList();

        if (!pendingList.Any())
        {
            _logger.LogInformation("Nenhum compromisso pendente para notificação diária.");
            return Result.Success();
        }

        var appointmentsByUser = pendingList.GroupBy(a => a.UserId);

        foreach (var userGroup in appointmentsByUser)
        {
            var userId = userGroup.Key;
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user == null || string.IsNullOrWhiteSpace(user.Email)) continue;

            var sb = new StringBuilder();
            sb.AppendLine("<h2>Sua Agenda de Hoje</h2>");
            sb.AppendLine("<ul>");
            foreach (var appt in userGroup.OrderBy(a => a.Date))
            {
                var time = appt.Date.ToLocalTime().ToString("HH:mm");
                sb.AppendLine($"<li><strong>{time}</strong> - {appt.Title} ({appt.Type})<br/>{appt.Description}</li>");
            }
            sb.AppendLine("</ul>");

            var queueItem = new EmailQueueItem(
                userId,
                "Usuário",
                user.Email,
                $"Agenda do Dia - {DateTime.UtcNow.ToLocalTime():dd/MM/yyyy}",
                sb.ToString(),
                DateTime.UtcNow,
                null,
                null);

            await _emailQueueRepository.AddAsync(queueItem, cancellationToken);

            // Mark as sent
            foreach (var appt in userGroup)
            {
                appt.DailyNotificationSent = true;
                await _appointmentRepository.UpdateAsync(appt, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Notificações diárias enfileiradas com sucesso.");

        return Result.Success();
    }

    public async Task<Result<IEnumerable<CreateAppointmentDto>>> ParseFromTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<IEnumerable<CreateAppointmentDto>>(new Error("Validation", "Text cannot be empty"));

        var currentYear = DateTime.Now.Year;
        var today = DateTime.Now.ToString("dd/MM/yyyy");

        var metaPrompt = $@"
Seu objetivo é analisar o texto do usuário contendo uma lista de compromissos ou um texto livre e extrair os dados.
Se o usuário informar um mês sem o ano (ex: 15/03), assuma que o ano atual é {currentYear}. Se o mês já tiver passado neste ano em relação à data atual ({today}), também assuma o ano de {currentYear} (assumindo que seja para o mesmo ano a menos que faça explícito sentido ser no próximo). Assuma que todos os compromissos são de ano corrente {currentYear}.

O usuário está no fuso horário de Brasília (UTC-3). Ao gerar o campo date, converta o horário informado para UTC somando 3 horas. Por exemplo, se o usuário mencionar 10:30, retorne T13:30:00.000Z. Se mencionar 08:00, retorne T11:00:00.000Z.

Regras do Array JSON:
Retorne uma lista de objetos contendo *apenas* as seguintes propriedades:
- title: string (nome ou título do compromisso, inclua aqui informações extras como observações, ex: 'Aplicação ferrosa clínica (R$ 160)')
- date: string no formato ISO 8601 UTC (ex: '2026-04-21T13:30:00.000Z' para 10:30 Brasília). Se a pessoa não definir uma hora exata, assuma às 08:00 Brasília = T11:00:00.000Z.
- type: string (Classifique baseado no título. Os tipos permitidos *estritamente* são: 'Medical' (médicos/clínicas/exames), 'HomeService' (serviços casa/compras), 'Payment' (pagar/cobrar), 'Other' (padrão se não couber nas demais)).
- description: string (opcional. use apenas se houver contexto extra).

TEXTO PARA EXTRAIR:
{text}";

        try
        {
            var jsonResult = await _promptGenerator.GenerateAsync(metaPrompt, "chatgpt", "json_extraction");

            // Clean up potential markdown formatting that the LLM might still throw
            jsonResult = jsonResult.Replace("```json", "").Replace("```", "").Trim();

            var startIndex = jsonResult.IndexOf('[');
            var endIndex = jsonResult.LastIndexOf(']');

            if (startIndex >= 0 && endIndex >= startIndex)
            {
                jsonResult = jsonResult.Substring(startIndex, endIndex - startIndex + 1);
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            var parsedAppointments = System.Text.Json.JsonSerializer.Deserialize<List<CreateAppointmentDto>>(jsonResult, options);

            if (parsedAppointments == null || !parsedAppointments.Any())
                return Result.Failure<IEnumerable<CreateAppointmentDto>>(new Error("Parsing", "Could not parse any appointments from the provided text."));

            return Result.Success(parsedAppointments.AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing appointments from text via AI");
            return Result.Failure<IEnumerable<CreateAppointmentDto>>(new Error("AI_Error", "Failed to parse text via AI: " + ex.Message));
        }
    }

    private static AppointmentDto MapToDto(Appointment entity)
    {
        return new AppointmentDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Date = entity.Date,
            Type = entity.Type,
            DurationMinutes = entity.DurationMinutes,
            DailyNotificationSent = entity.DailyNotificationSent,
            LabelId = entity.LabelId,
            Label = entity.Label != null ? new AppointmentLabelDto
            {
                Id = entity.Label.Id,
                Name = entity.Label.Name,
                Color = entity.Label.Color,
                Order = entity.Label.Order
            } : null,
            RecurrenceGroupId = entity.RecurrenceGroupId,
            IsCancelled = entity.IsCancelled
        };
    }
}
