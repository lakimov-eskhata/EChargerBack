namespace Domain.Enums;

public class TransactionStatusEnum(int value, string name) : BaseEnum<TransactionStatusEnum, int>(value, name)
{
    public static readonly TransactionStatusEnum InProgress = new(0, "InProgress");
    public static readonly TransactionStatusEnum Started = new(1, "Started");
    public static readonly TransactionStatusEnum Completed = new(2, "Completed");
    public static readonly TransactionStatusEnum Suspended = new(3, "Suspended");
    public static readonly TransactionStatusEnum Stopped = new(4, "Stopped");
};