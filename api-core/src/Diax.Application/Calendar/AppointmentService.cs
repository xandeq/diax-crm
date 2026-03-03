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
            UserId = userId.Value
        };

        await _appointmentRepository.AddAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(appointment));
    }

    public async Task<Result<AppointmentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken);

        if (appointment == null)
            return Result.Failure<AppointmentDto>(new Error("NotFound", "Appointment not found."));

        return Result.Success(MapToDto(appointment));
    }

    public async Task<Result<IEnumerable<AppointmentDto>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var appointments = await _appointmentRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        return Result.Success(appointments.Select(MapToDto));
    }

    public async Task<Result<AppointmentDto>> UpdateAsync(Guid id, UpdateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken);

        if (appointment == null)
            return Result.Failure<AppointmentDto>(new Error("NotFound", "Appointment not found."));

        if (dto.Title != null) appointment.Title = dto.Title;
        if (dto.Description != null) appointment.Description = dto.Description;
        if (dto.Date.HasValue) appointment.Date = dto.Date.Value;
        if (dto.Type.HasValue) appointment.Type = dto.Type.Value;

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

        var metaPrompt = $@"
Você é um extator inteligente de agendamentos. Seu objetivo é analisar o texto do usuário contendo uma lista de compromissos ou um texto livre e retornar *exclusivamente* um array JSON válido.
Se o usuário informar um mês sem o ano (ex: 15/03), assuma que o ano atual é {currentYear}. Se o mês já tiver passado neste ano em relação à data atual ({DateTime.Now:dd/MM/yyyy}), também assuma o ano de {currentYear} (assumindo que seja para o mesmo ano a menos que faça explícito sentido ser no próximo). Assuma que todos os compromissos são de ano corrente {currentYear}.

Regras do Array JSON:
Retorne uma lista de objetos contendo *apenas* as seguintes propriedades:
- title: string (nome ou título do compromisso, inclua aqui informações extras como observações, ex: 'Aplicação ferrosa clínica (R$ 160)')
- date: string no formato completo ISO 8601 UTC (ex: '2026-03-03T10:00:00.000Z'). Se a pessoa não definir uma hora exata, assuma às 08:00 da manhã.
- type: string (Classifique baseado no título. Os tipos permitidos *estritamente* são: 'Medical' (médicos/clínicas/exames), 'HomeService' (serviços casa/compras), 'Payment' (pagar/cobrar), 'Other' (padrão se não couber nas demais)).
- description: string (opcional. use apenas se houver contexto extra).

Retorne APENAS o JSON puro, sem formatações Markdown (sem ```json no começo).";

        try
        {
            var jsonResult = await _promptGenerator.GenerateAsync(text, "chatgpt", "json_extraction");

            // Clean up potential markdown formatting that the LLM might still throw
            jsonResult = jsonResult.Replace("```json", "").Replace("```", "").Trim();

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
            DailyNotificationSent = entity.DailyNotificationSent
        };
    }
}
