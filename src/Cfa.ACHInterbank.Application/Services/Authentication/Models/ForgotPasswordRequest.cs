namespace Cfa.ACHInterbank.Application.Services.Authentication.Models;

/// <summary>
/// Solicitud para iniciar el flujo de recuperación de contraseña.
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// Correo electrónico usado para solicitar la recuperación de la contraseña.
    /// </summary>
    public string? Email { get; set; }
}
