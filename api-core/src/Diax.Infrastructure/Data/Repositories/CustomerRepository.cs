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
        string? sortBy = null,
        bool sortDescending = false,
        bool? hasEmail = null,
        bool? hasWhatsApp = null,
        PersonType? personType = null,
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

        // Filtro: possui e-mail
        if (hasEmail.HasValue)
        {
            query = hasEmail.Value
                ? query.Where(c => c.Email != null && c.Email != "")
                : query.Where(c => c.Email == null || c.Email == "");
        }

        // Filtro: possui WhatsApp
        if (hasWhatsApp.HasValue)
        {
            query = hasWhatsApp.Value
                ? query.Where(c => c.WhatsApp != null && c.WhatsApp != "")
                : query.Where(c => c.WhatsApp == null || c.WhatsApp == "");
        }

        // Filtro: tipo de pessoa
        if (personType.HasValue)
        {
            query = query.Where(c => c.PersonType == personType.Value);
        }

        // Contagem total
        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting dinâmico
        IOrderedQueryable<Customer> orderedQuery = (sortBy?.ToLower()) switch
        {
            "name" => sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "email" => sortDescending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "status" => sortDescending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "companyname" => sortDescending ? query.OrderByDescending(c => c.CompanyName) : query.OrderBy(c => c.CompanyName),
            "createdat" => sortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            "leadscore" => sortDescending ? query.OrderByDescending(c => c.LeadScore) : query.OrderBy(c => c.LeadScore),
            "phone" => sortDescending ? query.OrderByDescending(c => c.Phone) : query.OrderBy(c => c.Phone),
            _ => query.OrderByDescending(c => c.CreatedAt) // Default: mais recentes primeiro
        };

        // Paginação
        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
