namespace Cfa.ACHInterbank.Domain.Models.ACH;

public static class AchReturnDirection
{
    public const string Any = "Any";
    public const string Incoming = "Incoming";
    public const string Outgoing = "Outgoing";
}

public static class AchReturnFlowType
{
    public const string Any = "Any";
    public const string Return = "Return";
    public const string ReturnOfReturn = "ReturnOfReturn";
    public const string FileRejection = "FileRejection";
}
