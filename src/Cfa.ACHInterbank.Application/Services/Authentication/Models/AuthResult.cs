namespace Cfa.ACHInterbank.Application.Services.Authentication.Models;

/// <summary>
/// Resultado producido por los flujos de autenticación y renovación de sesión.
/// </summary>
public class AuthResult
{
    /// <summary>
    /// Indica si la autenticación o renovación de sesión fue satisfactoria.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje asociado al resultado cuando la operación necesita comunicar una causa o estado.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Token JWT emitido cuando la autenticación o renovación es satisfactoria.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Fecha y hora de expiración calculada para el token JWT emitido.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Identificador de usuario asociado a la sesión autenticada.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Nombre completo asociado al usuario autenticado, cuando está disponible.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Roles asociados al usuario e incluidos en el resultado de autenticación.
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Permisos asociados al usuario e incluidos en el resultado de autenticación.
    /// </summary>
    public IEnumerable<string> Permissions { get; set; } = Enumerable.Empty<string>();
}
