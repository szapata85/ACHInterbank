namespace Cfa.ACHInterbank.Application.Services.Authentication.Models;

/// <summary>
/// Credenciales utilizadas para solicitar autenticación en ACHInterbank.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Identificador de usuario utilizado para buscar la cuenta que intenta autenticarse.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Contraseña proporcionada por el usuario para validar sus credenciales.
    /// </summary>
    public string? Password { get; set; }
}
