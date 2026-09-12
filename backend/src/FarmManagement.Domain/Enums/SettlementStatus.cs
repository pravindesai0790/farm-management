namespace FarmManagement.Domain.Enums;

public enum SettlementStatus
{
    NotReady = 1,
    ReadyForPayout = 2,
    PartiallyPaid = 3,
    Paid = 4,
    CarryForwardOnly = 5
}
