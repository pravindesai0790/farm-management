using System.Text.Json.Serialization;

namespace FarmManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PurchaseInvoiceStatus
{
    Draft = 1,
    Posted = 2,
    Reversed = 3
}
