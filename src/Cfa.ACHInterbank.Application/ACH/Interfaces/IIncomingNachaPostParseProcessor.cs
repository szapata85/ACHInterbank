namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaPostParseProcessor
{
    Task ProcessAsync(Guid ingestionId, string executedBy, CancellationToken ct = default);
}
