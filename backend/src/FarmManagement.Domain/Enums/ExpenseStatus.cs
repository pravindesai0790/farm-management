using System.Text.Json.Serialization;

namespace FarmManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExpenseStatus
{
    Draft = 1,
    Posted = 2,
    Reversed = 3
}
