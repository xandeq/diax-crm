namespace Diax.Application.Customers.Dtos;

public record BulkSanitizationRequest(IEnumerable<Guid>? CustomerIds = null);
