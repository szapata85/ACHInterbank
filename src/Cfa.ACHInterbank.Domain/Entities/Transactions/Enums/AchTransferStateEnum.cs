namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

public enum AchTransferStateEnum
{
    Pending = 1,
    ReturnedByOperator = 2,
    ReturnedByEpr = 3,
    AppliedTacitly = 4,
    Certified = 5
}
