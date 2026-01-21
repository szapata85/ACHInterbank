using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class ReturnReasonConfiguration : IEntityTypeConfiguration<ReturnReason>
{
    public void Configure(EntityTypeBuilder<ReturnReason> builder)
    {
        builder.ToTable("ReturnReasons");

        builder.HasKey(reason => reason.Id);

        builder.Property(reason => reason.Code)
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(reason => reason.Description)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(reason => reason.Category)
            .HasMaxLength(1)
            .IsRequired();

        builder.HasIndex(reason => reason.Code)
            .IsUnique();

        builder.HasData(GetSeedData());
    }

    private static IEnumerable<ReturnReason> GetSeedData()
    {
        return new List<ReturnReason>
        {
            new() { Id = 1, Code = "R01", Description = "Fondos Insuficientes.", Category = "R" },
            new() { Id = 2, Code = "R02", Description = "Cuenta o Depósito Electrónico Cerrado (Saldado o Cancelado).", Category = "R" },
            new() { Id = 3, Code = "R03", Description = "Cuenta o Depósito Electrónico No Abierto.", Category = "R" },
            new() { Id = 4, Code = "R04", Description = "Número Cuenta o Depósito Electrónico Inválido.", Category = "R" },
            new() { Id = 5, Code = "R06", Description = "Devolución Solicitada por el participante originador (por error o listas restrictivas).", Category = "R" },
            new() { Id = 6, Code = "R07", Description = "Autorización de Recaudo Revocada por el usuario Receptor.", Category = "R" },
            new() { Id = 7, Code = "R08", Description = "Orden de No Pago (instrucción específica del usuario).", Category = "R" },
            new() { Id = 8, Code = "R09", Description = "Fondos no Disponibles (saldo total suficiente, pero no el disponible).", Category = "R" },
            new() { Id = 9, Code = "R10", Description = "No existe prenotificación (para débitos).", Category = "R" },
            new() { Id = 10, Code = "R12", Description = "Usuario Originador no autorizado o Sucursal Vendida.", Category = "R" },
            new() { Id = 11, Code = "R13", Description = "Devolución por solicitud del usuario Receptor (Persona Natural: fecha/monto errado o duplicidad).", Category = "R" },
            new() { Id = 12, Code = "R14", Description = "Muerte del delegado o Representante.", Category = "R" },
            new() { Id = 13, Code = "R15", Description = "Muerte del Beneficiario o Titular de la Cuenta.", Category = "R" },
            new() { Id = 14, Code = "R16", Description = "Cuenta o Depósito Electrónico Inactivo / Bloqueado.", Category = "R" },
            new() { Id = 15, Code = "R17", Description = "La Identificación no coincide con Cuenta.", Category = "R" },
            new() { Id = 16, Code = "R20", Description = "Cuenta No Habilitada (listas restrictivas OFAC/ONU o fines políticos).", Category = "R" },
            new() { Id = 17, Code = "R23", Description = "Devolución crédito por solicitud del usuario (sobrepago, litigio, etc.).", Category = "R" },
            new() { Id = 18, Code = "R29", Description = "Devolución débito por solicitud del usuario (Persona Jurídica).", Category = "R" },
            new() { Id = 19, Code = "R30", Description = "Cliente Receptor no habilitado para depósitos electrónicos.", Category = "R" },
            new() { Id = 20, Code = "R31", Description = "Prenotificación no procesada por falta de información en adenda.", Category = "R" },
            new() { Id = 21, Code = "R32", Description = "Transacción crédito no procesada por falta de información en adenda.", Category = "R" },
            new() { Id = 22, Code = "R33", Description = "Excede los límites establecidos para Depósitos Electrónicos.", Category = "R" },
            new() { Id = 23, Code = "R35", Description = "Tipo de Cuenta Errada.", Category = "R" },
            new() { Id = 24, Code = "D01", Description = "Fecha efectiva menor a la fecha de proceso.", Category = "D" },
            new() { Id = 25, Code = "D02", Description = "Fecha efectiva no es válida.", Category = "D" },
            new() { Id = 26, Code = "D03", Description = "Valor de la transacción no es numérico.", Category = "D" },
            new() { Id = 27, Code = "D04", Description = "Valor de prenotificación es diferente de cero.", Category = "D" },
            new() { Id = 28, Code = "D05", Description = "Valor de transacción monetaria es igual a cero.", Category = "D" },
            new() { Id = 29, Code = "D06", Description = "Excede el monto diario permitido por usuario/cuenta.", Category = "D" },
            new() { Id = 30, Code = "D07", Description = "Transacción excede el límite y no fue autorizada.", Category = "D" },
            new() { Id = 31, Code = "D08", Description = "Número de secuencia del registro adenda incorrecto.", Category = "D" },
            new() { Id = 32, Code = "D09", Description = "Indicador de adenda no concuerda con el registro.", Category = "D" },
            new() { Id = 33, Code = "D10", Description = "Causal de devolución en adenda es incorrecta.", Category = "D" },
            new() { Id = 34, Code = "D11", Description = "Número mínimo de registros adenda diferente de 1.", Category = "D" },
            new() { Id = 35, Code = "D12", Description = "Código de tipo de registro adenda es incorrecto (debe ser 05 o 99).", Category = "D" },
            new() { Id = 36, Code = "D13", Description = "Datos obligatorios en débito (referencia/servicio) vacíos o en cero.", Category = "D" },
            new() { Id = 37, Code = "D14", Description = "Código de cliente originador por servicio no numérico.", Category = "D" },
            new() { Id = 38, Code = "D15", Description = "Nombre del usuario originador vacío.", Category = "D" },
            new() { Id = 39, Code = "D16", Description = "ID del originador en control de lote no coincide con encabezado.", Category = "D" },
            new() { Id = 40, Code = "D17", Description = "Número de cuenta receptora no válida.", Category = "D" },
            new() { Id = 41, Code = "D18", Description = "Número de lote en control no coincide con encabezado.", Category = "D" },
            new() { Id = 42, Code = "D19", Description = "Código de participante originador vacío o en ceros.", Category = "D" },
            new() { Id = 43, Code = "D20", Description = "Código clase de transacción en control de lote inválido.", Category = "D" },
            new() { Id = 44, Code = "D21", Description = "Código de participante receptor no válido o no está en producción.", Category = "D" },
            new() { Id = 45, Code = "D22", Description = "Nombre del cliente receptor vacío.", Category = "D" },
            new() { Id = 46, Code = "D23", Description = "ID del usuario receptor vacío.", Category = "D" },
            new() { Id = 47, Code = "D24", Description = "Descripción del lote vacía.", Category = "D" },
            new() { Id = 48, Code = "D25", Description = "Descripción de lote de Cuentas de Préstamo no válida.", Category = "D" },
            new() { Id = 49, Code = "D26", Description = "Campo alfanumérico no alineado a la izquierda.", Category = "D" },
            new() { Id = 50, Code = "D27", Description = "Caracteres especiales inválidos en la línea.", Category = "D" },
            new() { Id = 51, Code = "D28", Description = "Devolución de una devolución.", Category = "D" },
            new() { Id = 52, Code = "D29", Description = "Devolución débito tardía.", Category = "D" },
            new() { Id = 53, Code = "D30", Description = "Entidad Participante no puede procesar débitos.", Category = "D" },
            new() { Id = 54, Code = "D31", Description = "Lote duplicado en el mismo día sin autorización.", Category = "D" },
            new() { Id = 55, Code = "D32", Description = "Transacción no permitida en este ciclo.", Category = "D" }
        };
    }
}
