using System.Text.Json.Serialization;

namespace Diax.Domain.Tasks;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskItemStatus
{
    Todo = 1,
    InProgress = 2,
    Done = 3,
    Cancelled = 4
}
