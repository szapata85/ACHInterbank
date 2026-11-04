namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Configuración exclusiva CENIT. ProcessingConnected es siempre false en I01.</summary>
public sealed record CenitAutomaticReturnSettings(string Mode, bool ProcessingConnected);

/// <summary>ExpectedMode se obtiene del GET previo para rechazar cambios de sesión obsoletos.</summary>
public sealed record UpdateCenitAutomaticReturnSettings(string Mode, string ExpectedMode);
