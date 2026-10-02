using System.Text.Json.Serialization;

namespace FarmManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoicePaymentStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Reversed = 4
}
