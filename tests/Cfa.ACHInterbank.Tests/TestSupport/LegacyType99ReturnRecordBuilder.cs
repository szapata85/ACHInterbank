namespace Cfa.ACHInterbank.Tests.TestSupport;

internal static class LegacyType99ReturnRecordBuilder
{
    internal const int RecordLength = 106;
    internal const int RecordTypePosition = 1;
    internal const int AddendaTypePosition = 2;
    internal const int ReturnReasonPosition = 4;
    internal const int ReturnReasonLength = 3;
    internal const int OriginalTracePosition = 7;
    internal const int OriginalTraceLength = 15;
    internal const int AddendaTracePosition = 92;
    internal const int AddendaTraceLength = 15;

    internal static string Build(string returnReason, string originalTrace, string? addendaTrace = null)
    {
        ValidateLength(returnReason, ReturnReasonLength, nameof(returnReason));
        ValidateLength(originalTrace, OriginalTraceLength, nameof(originalTrace));
        if (addendaTrace is not null)
        {
            ValidateLength(addendaTrace, AddendaTraceLength, nameof(addendaTrace));
        }

        var record = new string(' ', RecordLength).ToCharArray();
        Write(record, RecordTypePosition, "7");
        Write(record, AddendaTypePosition, "99");
        Write(record, ReturnReasonPosition, returnReason);
        Write(record, OriginalTracePosition, originalTrace);
        if (addendaTrace is not null)
        {
            Write(record, AddendaTracePosition, addendaTrace);
        }

        return new string(record);
    }

    private static void ValidateLength(string? value, int expectedLength, string parameterName)
    {
        if (value is null || value.Length != expectedLength)
        {
            throw new ArgumentException($"{parameterName} must contain exactly {expectedLength} characters.", parameterName);
        }
    }

    private static void Write(char[] record, int physicalPosition, string value)
        => value.CopyTo(0, record, physicalPosition - 1, value.Length);
}
