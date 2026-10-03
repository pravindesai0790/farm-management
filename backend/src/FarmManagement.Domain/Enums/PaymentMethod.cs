using System.Text.Json.Serialization;

namespace FarmManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Upi = 3,
    Cheque = 4,
    Other = 5
}
