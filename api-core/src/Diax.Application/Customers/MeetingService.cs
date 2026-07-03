using Diax.Application.Common;
using Diax.Application.Notifications;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Tasks;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers;

public record PublicBookingRequest(
    Guid UserId,
    DateTime ScheduledAt,
    string Name,
    string Email,
    string? Phone,
    string? Notes);

public record MeetingDto(
    Guid Id,
    Guid? CustomerId,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    string? Notes,
    DateTime ScheduledAt,
    int DurationMinutes,
    MeetingStatus Status);

public record AvailabilityDayDto(string Date, List<DateTime> Slots);

/// <summary>
/// Agendamento de reuniões: disponibilidade em slots de 30min (horário comercial
/// América/São_Paulo, dias úteis) e reserva pelo link público.
/// </summary>
public class MeetingService : IApplicationService
{
    // Horário comercial em BRT (UTC-3, sem horário de verão desde 2019)
    public const int BusinessStartHourBrt = 9;
    public const int BusinessEndHourBrt = 18;
    public const int SlotMinutes = Meeting.DefaultDurationMinutes;
    public static readonly TimeSpan BrtOffset = TimeSpan.FromHours(-3);
    /// <summary>Antecedência mínima para reservar (evita reunião daqui a 10 minutos).</summary>
    public static readonly TimeSpan MinimumLeadTime = TimeSpan.FromHours(4);
    public const int MaxDaysAhead = 14;

    private readonly IMeetingRepository _meetingRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITelegramSender _telegramSender;
    private readonly ILogger<MeetingService> _logger;

    public MeetingService(
        IMeetingRepository meetingRepository,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        ITaskRepository taskRepository,
        IUnitOfWork unitOfWork,
        ITelegramSender telegramSender,
        ILogger<MeetingService> logger)
    {
        _meetingRepository = meetingRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
        _telegramSender = telegramSender;
        _logger = logger;
    }

    /// <summary>
    /// Slots livres (UTC) nos próximos <paramref name="days"/> dias — função pura
    /// sobre 'agora' e a lista de horários já reservados.
    /// </summary>
    public static List<DateTime> ComputeAvailableSlots(
        DateTime nowUtc, IReadOnlyCollection<DateTime> bookedSlotsUtc, int days)
    {
        days = Math.Clamp(days, 1, MaxDaysAhead);
        var booked = bookedSlotsUtc.ToHashSet();
        var earliest = nowUtc + MinimumLeadTime;
        var slots = new List<DateTime>();

        var todayBrt = (nowUtc + BrtOffset).Date;
        for (var d = 0; d < days; d++)
        {
            var dayBrt = todayBrt.AddDays(d);
            if (dayBrt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            for (var hour = BusinessStartHourBrt; hour < BusinessEndHourBrt; hour++)
            {
                for (var minute = 0; minute < 60; minute += SlotMinutes)
                {
                    var slotUtc = DateTime.SpecifyKind(
                        dayBrt.AddHours(hour).AddMinutes(minute) - BrtOffset, DateTimeKind.Utc);
                    if (slotUtc >= earliest && !booked.Contains(slotUtc))
                        slots.Add(slotUtc);
                }
            }
        }

        return slots;
    }

    public async Task<Result<List<AvailabilityDayDto>>> GetPublicAvailabilityAsync(
        Guid userId, int days, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
            return Result.Failure<List<AvailabilityDayDto>>(Error.NotFound("Agenda", userId.ToString()));

        days = Math.Clamp(days, 1, MaxDaysAhead);
        var now = DateTime.UtcNow;
        var booked = await _meetingRepository.GetConfirmedInRangeAsync(
            userId, now, now.AddDays(days + 1), ct);

        var slots = ComputeAvailableSlots(now, booked.Select(m => m.ScheduledAt).ToList(), days);

        // Agrupa por dia BRT para a UI montar as colunas
        var grouped = slots
            .GroupBy(s => (s + BrtOffset).Date)
            .OrderBy(g => g.Key)
            .Select(g => new AvailabilityDayDto(g.Key.ToString("yyyy-MM-dd"), g.OrderBy(s => s).ToList()))
            .ToList();

        return grouped;
    }

    public async Task<Result<MeetingDto>> BookPublicAsync(PublicBookingRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return Result.Failure<MeetingDto>(Error.NotFound("Agenda", request.UserId.ToString()));

        var slotUtc = request.ScheduledAt.Kind == DateTimeKind.Utc
            ? request.ScheduledAt
            : DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc);

        // Revalida contra a disponibilidade real (regra + já reservados)
        var now = DateTime.UtcNow;
        var booked = await _meetingRepository.GetConfirmedInRangeAsync(
            request.UserId, now, now.AddDays(MaxDaysAhead + 1), ct);
        var available = ComputeAvailableSlots(now, booked.Select(m => m.ScheduledAt).ToList(), MaxDaysAhead);
        if (!available.Contains(slotUtc))
            return Result.Failure<MeetingDto>(Error.Validation(
                "ScheduledAt", "Este horário não está mais disponível — escolha outro."));

        Meeting meeting;
        try
        {
            meeting = new Meeting(request.UserId, slotUtc, request.Name, request.Email, request.Phone, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<MeetingDto>(Error.Validation("Meeting", ex.Message));
        }

        // Match do lead por email → vincula e registra contato
        var customer = (await _customerRepository.FindAsync(
            c => c.Email == meeting.ContactEmail, ct)).FirstOrDefault();
        if (customer != null)
        {
            meeting.LinkCustomer(customer.Id);
            customer.RegisterContact();
            await _customerRepository.UpdateAsync(customer, ct);
        }

        await _meetingRepository.AddAsync(meeting, ct);

        // Task para o dono ver a reunião na lista de Tarefas
        var brt = slotUtc + BrtOffset;
        var contact = string.Join(" · ", new[]
        {
            $"Email: {meeting.ContactEmail}",
            meeting.ContactPhone != null ? $"Tel: {meeting.ContactPhone}" : null,
            meeting.Notes != null ? $"Obs: {meeting.Notes}" : null,
        }.Where(s => s != null));
        var description = contact.Length > 256 ? contact[..256] : contact;

        await _taskRepository.AddAsync(new TaskItem
        {
            Title = $"📅 Reunião: {meeting.ContactName} — {brt:dd/MM HH:mm}",
            Description = description,
            Priority = TaskItemPriority.High,
            DueDate = slotUtc,
            UserId = request.UserId,
            CustomerId = meeting.CustomerId,
        }, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("UX_meetings_user_slot") == true
            || ex.Message.Contains("UX_meetings_user_slot"))
        {
            // Corrida: outro visitante levou o slot entre a validação e o INSERT
            return Result.Failure<MeetingDto>(Error.Validation(
                "ScheduledAt", "Este horário acabou de ser reservado — escolha outro."));
        }

        _logger.LogInformation(
            "Reunião agendada: {MeetingId} {Contact} em {SlotBrt} BRT (customer: {CustomerId})",
            meeting.Id, meeting.ContactEmail, brt, meeting.CustomerId);

        // 🔔 Avisa o dono na hora (fire-safe — falha no Telegram não quebra a reserva)
        try
        {
            static string Esc(string s) => s
                .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            await _telegramSender.SendAsync(
                $"📅 <b>Nova reunião agendada!</b>\n" +
                $"{Esc(meeting.ContactName)} — <b>{brt:dd/MM} às {brt:HH:mm}</b> (Brasília)\n" +
                $"Email: {Esc(meeting.ContactEmail)}" +
                (meeting.ContactPhone != null ? $" · Tel: {WhatsAppLink.AsHtml(meeting.ContactPhone)}" : "") +
                (meeting.Notes != null ? $"\nAssunto: {Esc(meeting.Notes)}" : "") +
                (meeting.CustomerId != null ? "\n<i>Lead já existente no CRM — contato registrado.</i>" : ""), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao notificar Telegram sobre a reunião (ignorada)");
        }

        return ToDto(meeting);
    }

    public async Task<List<MeetingDto>> ListUpcomingAsync(Guid userId, CancellationToken ct = default)
    {
        var meetings = await _meetingRepository.GetUpcomingAsync(userId, 50, ct);
        return meetings.Select(ToDto).ToList();
    }

    public async Task<Result<MeetingDto>> CancelAsync(Guid meetingId, Guid userId, CancellationToken ct = default)
    {
        var meeting = await _meetingRepository.GetByIdAsync(meetingId, ct);
        if (meeting == null || meeting.UserId != userId)
            return Result.Failure<MeetingDto>(Error.NotFound("Meeting", meetingId.ToString()));

        try
        {
            meeting.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<MeetingDto>(Error.Validation("Meeting", ex.Message));
        }

        await _meetingRepository.UpdateAsync(meeting, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(meeting);
    }

    private static MeetingDto ToDto(Meeting m) => new(
        m.Id, m.CustomerId, m.ContactName, m.ContactEmail, m.ContactPhone,
        m.Notes, m.ScheduledAt, m.DurationMinutes, m.Status);
}
