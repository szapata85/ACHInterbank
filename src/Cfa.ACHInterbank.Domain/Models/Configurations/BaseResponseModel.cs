namespace Cfa.ACHInterbank.Domain.Models.Configurations;

/// <summary>Envelope HTTP utilizado por operaciones de la API para exponer estado, mensaje y datos.</summary>
public class BaseResponseModel
{
    /// <summary>Código de estado HTTP reflejado por el envelope.</summary>
    public int StatusCode { get; set; }

    /// <summary>Indica si el código de estado se encuentra en el rango 2xx.</summary>
    public bool Sucess { get; set; }

    /// <summary>Mensaje asociado al resultado, cuando existe.</summary>
    public string? Message { get; set; }

    /// <summary>Datos del resultado. Su forma concreta depende de la operación que construye el envelope.</summary>
    public dynamic? Data { get; set; }
}
