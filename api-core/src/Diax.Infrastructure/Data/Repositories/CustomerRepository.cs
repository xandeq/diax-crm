using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório de Customer.
/// </summary>
public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Document == document, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByStatusAsync(
        CustomerStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetLeadsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status < CustomerStatus.Customer)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == CustomerStatus.Customer)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetBySourceAsync(
        LeadSource source,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Source == source)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.Email == email);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        CustomerStatus? status = null,
        LeadSource? source = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        // Filtro por busca (nome ou e-mail)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(searchLower) ||
                c.Email.ToLower().Contains(searchLower) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(searchLower)));
        }

        // Filtro por status
        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        // Filtro por origem
        if (source.HasValue)
        {
            query = query.Where(c => c.Source == source.Value);
        }

        // Contagem total
        var totalCount = await query.CountAsync(cancellationToken);

        // Paginação
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
