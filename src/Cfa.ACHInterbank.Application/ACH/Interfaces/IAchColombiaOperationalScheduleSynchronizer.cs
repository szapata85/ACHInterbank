namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>
/// Sincroniza la programación operacional persistida de ACH Colombia con el scheduler de runtime.
/// La persistencia de la configuración debe ocurrir antes de invocar este puerto.
/// </summary>
public interface IAchColombiaOperationalScheduleSynchronizer
{
    Task SynchronizeOutboundAsync(CancellationToken ct = default);
}
