using Diax.Domain.ImageGeneration;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ImageTemplateRepository : Repository<ImageTemplate>, IImageTemplateRepository
{
    public ImageTemplateRepository(DiaxDbContext context) : base(context) { }

    public async Task<List<ImageTemplate>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ImageTemplate>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.IsEnabled && x.Category == category)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
