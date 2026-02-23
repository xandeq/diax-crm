using Diax.Domain.ImageGeneration;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class ImageGenerationProjectRepository : Repository<ImageGenerationProject>, IImageGenerationProjectRepository
{
    public ImageGenerationProjectRepository(DiaxDbContext context) : base(context) { }

    public async Task<ImageGenerationProject?> GetByIdWithImagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.GeneratedImages)
            .Include(x => x.Template)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<ImageGenerationProject>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Include(x => x.Template)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(x => x.UserId == userId, cancellationToken);
    }
}
