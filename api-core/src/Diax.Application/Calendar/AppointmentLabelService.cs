using Diax.Application.Calendar.Dtos;
using Diax.Domain.Auth;
using Diax.Domain.Calendar;
using Diax.Domain.Common;
using Diax.Shared.Results;

namespace Diax.Application.Calendar;

public class AppointmentLabelService : IAppointmentLabelService
{
    private readonly IAppointmentLabelRepository _labelRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AppointmentLabelService(
        IAppointmentLabelRepository labelRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _labelRepository = labelRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IEnumerable<AppointmentLabelDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Result.Failure<IEnumerable<AppointmentLabelDto>>(new Error("Unauthorized", "User is not authenticated."));

        var labels = await _labelRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        return Result.Success(labels.Select(MapToDto));
    }

    public async Task<Result<AppointmentLabelDto>> CreateAsync(CreateAppointmentLabelDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Result.Failure<AppointmentLabelDto>(new Error("Unauthorized", "User is not authenticated."));

        var label = new AppointmentLabel
        {
            Name = dto.Name,
            Color = dto.Color,
            Order = dto.Order,
            UserId = userId.Value
        };

        await _labelRepository.AddAsync(label, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(label));
    }

    public async Task<Result<AppointmentLabelDto>> UpdateAsync(Guid id, CreateAppointmentLabelDto dto, CancellationToken cancellationToken = default)
    {
        var label = await _labelRepository.GetByIdAsync(id, cancellationToken);
        if (label == null)
            return Result.Failure<AppointmentLabelDto>(new Error("NotFound", "Label not found."));

        label.Name = dto.Name;
        label.Color = dto.Color;
        label.Order = dto.Order;

        await _labelRepository.UpdateAsync(label, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(label));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var label = await _labelRepository.GetByIdAsync(id, cancellationToken);
        if (label == null)
            return Result.Failure(new Error("NotFound", "Label not found."));

        await _labelRepository.DeleteAsync(label, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static AppointmentLabelDto MapToDto(AppointmentLabel entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Color = entity.Color,
        Order = entity.Order
    };
}
