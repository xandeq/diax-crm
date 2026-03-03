using System.Text.Json.Serialization;

namespace Diax.Domain.Calendar;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppointmentType
{
    Medical = 1,
    HomeService = 2,
    Payment = 3,
    Other = 99
}
