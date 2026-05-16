using System.Text.Json.Serialization;

namespace Diax.Domain.Tasks;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskItemPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}
