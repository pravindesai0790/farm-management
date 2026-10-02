using System.Text.Json.Serialization;

namespace FarmManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceDueStatus
{
    NoDueDate = 1,
    NotDue = 2,
    DueToday = 3,
    Overdue = 4
}
