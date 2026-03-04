namespace Diax.Application.Customers.Dtos;

public class BulkDeleteRequest
{
    public List<Guid> Ids { get; set; } = [];
}

public class BulkDeleteResponse
{
    public int DeletedCount { get; set; }
}
