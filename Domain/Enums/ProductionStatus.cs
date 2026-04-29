namespace CarAssemblyErp.Domain.Enums;

public enum ProductionStatus
{
    Draft = 0,
    MaterialChecked = 1,
    MaterialShortage = 2,
    Ready = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6
}

public enum TransactionType
{
    Inbound = 0,
    Outbound = 1,
    ProductionConsume = 2,
    ProductionOutput = 3
}
