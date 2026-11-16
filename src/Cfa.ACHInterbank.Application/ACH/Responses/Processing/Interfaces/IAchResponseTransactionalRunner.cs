namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;

/// <summary>Runs DB-only prenotification response processing in one durable transaction.
/// No remote SOAP call is allowed inside the callback.</summary>
public interface IAchResponseTransactionalRunner
{
    Task<T> RunAsync<T>(Func<Task<T>> databaseWork, CancellationToken cancellationToken);
}
