using System.Text.Json.Serialization;

namespace FarmManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceLineType
{
    InventoryItem = 1,
    NonInventoryExpense = 2
}
