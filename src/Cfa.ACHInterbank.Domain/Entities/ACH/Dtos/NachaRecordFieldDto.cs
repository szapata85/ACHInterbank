namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

/// <summary>
/// Describe un campo de un layout NACHA-M legacy de ancho fijo.
/// </summary>
public class NachaRecordFieldDto
{
    /// <summary>Identificador interno persistido del campo legacy.</summary>
    public int Id { get; set; }

    /// <summary>Nombre lógico del campo usado por el renderer legacy como alternativa de resolución del valor.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Posición inicial basada en 1 usada por el renderer para ubicar el campo en el buffer.</summary>
    public int StartPosition { get; set; }

    /// <summary>Longitud configurada del campo; el renderer trunca o completa el valor a esta longitud.</summary>
    public int Length { get; set; }

    /// <summary>Carácter de relleno representado como texto; el modelo persistido utiliza un único carácter.</summary>
    public string PadChar { get; set; } = " ";

    /// <summary>Justificación del valor en el campo: R aplica relleno a la izquierda y cualquier otro valor usa relleno a la derecha; el valor legacy habitual es L.</summary>
    public string Justification { get; set; } = "L";

    /// <summary>Identificador de propiedad, clave o constante usado por el renderer legacy para resolver el valor fuente.</summary>
    public string DbColumn { get; set; } = string.Empty;

    /// <summary>Formato opcional aplicado por el renderer al convertir el valor a texto.</summary>
    public string? Format { get; set; }
}
