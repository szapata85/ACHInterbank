namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Dtos;

/// <summary>Parámetro clave/valor persistido y asociado a una definición de tarea.</summary>
public class TaskParameterDto
{
    /// <summary>Identificador numérico persistido del parámetro.</summary>
    public int Id { get; set; }

    /// <summary>Clave del parámetro; el servicio descarta elementos con clave vacía y recorta espacios al persistir.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Valor del parámetro; el servicio recorta espacios al persistir y usa cadena vacía cuando el valor recibido es null.</summary>
    public string Value { get; set; } = string.Empty;
}
