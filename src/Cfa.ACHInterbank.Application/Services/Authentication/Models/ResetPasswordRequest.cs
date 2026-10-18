namespace Cfa.ACHInterbank.Application.Services.Authentication.Models;

/// <summary>
/// Datos necesarios para completar un restablecimiento de contraseña previamente solicitado.
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// Token de recuperación que identifica la solicitud de restablecimiento pendiente.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Nueva contraseña que se almacenará si la solicitud puede completarse.
    /// </summary>
    public string? NewPassword { get; set; }

    /// <summary>
    /// Confirmación de la nueva contraseña; debe coincidir con <see cref="NewPassword"/>.
    /// </summary>
    public string? ConfirmPassword { get; set; }
}
