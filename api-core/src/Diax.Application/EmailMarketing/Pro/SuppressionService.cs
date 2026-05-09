using Diax.Application.Common;
using Diax.Domain.Common;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;

namespace Diax.Application.EmailMarketing.Pro;

public class SuppressionService : ISuppressionService
{
    private readonly IEmailSuppressionRepository _suppressionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public SuppressionService(
        IEmailSuppressionRepository suppressionRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _suppressionRepository = suppressionRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public Task<bool> IsSuppressedAsync(string email, CancellationToken cancellationToken = default)
        => _suppressionRepository.IsSuppressedAsync(_currentUserService.UserId!.Value, email, cancellationToken);

    public Task<List<EmailSuppression>> GetAllAsync(CancellationToken cancellationToken = default)
        => _suppressionRepository.GetAllAsync(_currentUserService.UserId!.Value, cancellationToken);

    public async Task SuppressEmailAsync(string email, SuppressionReason reason, string source, CancellationToken cancellationToken = default)
    {
        var existing = await _suppressionRepository.FindByEmailAsync(_currentUserService.UserId!.Value, email, cancellationToken);
        if (existing != null) return;

        var suppression = EmailSuppression.ForEmail(_currentUserService.UserId.Value, email, reason, source);
        await _suppressionRepository.AddAsync(suppression, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SuppressDomainAsync(string domain, SuppressionReason reason, string source, CancellationToken cancellationToken = default)
    {
        var suppression = EmailSuppression.ForDomain(_currentUserService.UserId!.Value, domain, reason, source);
        await _suppressionRepository.AddAsync(suppression, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid suppressionId, CancellationToken cancellationToken = default)
    {
        await _suppressionRepository.DeleteAsync(suppressionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
