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
    TransferOut = 7,

    // Compensating Reversal Types
    OpeningStockReversal = 8,
    ReceiptReversal = 9,
    IssueReversal = 10,
    AdjustmentInReversal = 11,
    AdjustmentOutReversal = 12,
    TransferInReversal = 13,
    TransferOutReversal = 14
}
