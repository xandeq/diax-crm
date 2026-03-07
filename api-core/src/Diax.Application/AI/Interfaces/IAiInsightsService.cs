using System;
using System.Threading;
using System.Threading.Tasks;

namespace Diax.Application.AI.Interfaces;

public interface IAiInsightsService
{
    Task<string> GetDailyInsightsAsync(Guid userId, CancellationToken cancellationToken = default);
}
