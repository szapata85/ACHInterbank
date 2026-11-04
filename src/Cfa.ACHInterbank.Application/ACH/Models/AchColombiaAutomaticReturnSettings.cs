namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Configuración ACH Colombia. ProcessingConnected informa únicamente
/// generación de archivos interna habilitada, nunca transmisión ni liquidación.</summary>
public sealed record AchColombiaAutomaticReturnSettings(string Mode, bool ProcessingConnected);

public sealed record UpdateAchColombiaAutomaticReturnSettings(string Mode);
