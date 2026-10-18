namespace Cfa.ACHInterbank.Application.Integrations.Exceptions;

public sealed class IntegrationMappingRevisionConflictException : InvalidOperationException
{
    public IntegrationMappingRevisionConflictException(long expectedRevision, long currentRevision)
        : base($"La configuración cambió desde que se abrió la edición. Revisión esperada={expectedRevision}, actual={currentRevision}.")
    {
        ExpectedRevision = expectedRevision;
        CurrentRevision = currentRevision;
    }

    public long ExpectedRevision { get; }
    public long CurrentRevision { get; }
}
