using System.Text.Json.Serialization;

namespace FarmManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StockMovementType
{
    OpeningStock = 1,
    Receipt = 2,
    Issue = 3,
    AdjustmentIn = 4,
    AdjustmentOut = 5,
    TransferIn = 6,
    TransferOut = 7
}
