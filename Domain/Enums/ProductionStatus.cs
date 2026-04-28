namespace CarAssemblyErp.Domain.Enums;

public enum ProductionStatus
{
    Draft = 0,
    MaterialShortage = 1,
    Ready = 2,
    InProgress = 3,
    Completed = 4
}

public enum TransactionType
{
    Inbound = 0,
    Outbound = 1,
    ProductionConsume = 2,
    ProductionOutput = 3
}
