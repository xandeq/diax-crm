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

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IEmailQueueRepository emailQueueRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<AppointmentService> logger)
    {
        _appointmentRepository = appointmentRepository;
        _emailQueueRepository = emailQueueRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
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
