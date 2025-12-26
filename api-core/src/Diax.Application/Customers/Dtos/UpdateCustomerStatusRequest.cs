using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.Dtos;

/// <summary>
/// DTO para atualização do status de um Customer/Lead.
/// </summary>
public class UpdateCustomerStatusRequest
{
    public CustomerStatus Status { get; set; }
}
