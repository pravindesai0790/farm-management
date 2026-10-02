using System.Text.Json.Serialization;

namespace FarmManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SupplierPaymentStatus
{
    Completed = 1,
    Reversed = 2
}
