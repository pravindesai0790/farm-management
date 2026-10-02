using System.Text.Json.Serialization;

namespace FarmManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceReceiptStatus
{
    NotReceived = 1,
    PartiallyReceived = 2,
    FullyReceived = 3,
    NotApplicable = 4
}
