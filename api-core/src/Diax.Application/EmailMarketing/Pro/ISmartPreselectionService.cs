using Diax.Application.EmailMarketing.Pro.Dtos;

namespace Diax.Application.EmailMarketing.Pro;

public interface ISmartPreselectionService
{
    Task<SmartPreselectResponse> PreselecAsync(SmartPreselectRequest request, CancellationToken cancellationToken = default);
}
