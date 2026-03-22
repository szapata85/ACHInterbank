using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaFormatValidator : INachaFormatValidator
{
    private const int RecordLength = 106;
    private static readonly HashSet<char> AllowedRecordCodes = ['1', '5', '6', '7', '8', '9'];

    public void ValidateOrThrow(string nachaContent, NachaValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(nachaContent))
        {
            throw new InvalidOperationException("El archivo NACHA-M está vacío.");
        }

        if (nachaContent.Length % RecordLength != 0)
        {
            throw new InvalidOperationException($"El archivo NACHA-M debe estar compuesto por registros fijos de {RecordLength} caracteres.");
        }

        var records = Chunk(nachaContent);
        if (records.Count == 0)
        {
            throw new InvalidOperationException("No se encontraron registros NACHA-M.");
        }

        ValidateRecordTypes(records);
        ValidateType5BusinessRules(records, context);
        ValidateCenitFileHeader(records, context);
    }

    private static List<string> Chunk(string nachaContent)
    {
        var records = new List<string>(nachaContent.Length / RecordLength);
        for (var index = 0; index < nachaContent.Length; index += RecordLength)
        {
            records.Add(nachaContent.Substring(index, RecordLength));
        }

        return records;
    }

    private static void ValidateRecordTypes(IReadOnlyList<string> records)
    {
        if (records[0][0] != '1')
        {
            throw new InvalidOperationException("El archivo NACHA-M debe iniciar con registro tipo 1.");
        }

        if (records[^1][0] != '9')
        {
            throw new InvalidOperationException("El archivo NACHA-M debe finalizar con registro tipo 9.");
        }

        foreach (var record in records)
        {
            if (record.Length != RecordLength)
            {
                throw new InvalidOperationException($"Se detectó un registro con longitud distinta de {RecordLength}.");
            }

            if (!AllowedRecordCodes.Contains(record[0]))
            {
                throw new InvalidOperationException($"Tipo de registro NACHA-M no soportado: {record[0]}.");
            }
        }
    }

    private static void ValidateType5BusinessRules(IReadOnlyList<string> records, NachaValidationContext context)
    {
        foreach (var record in records.Where(record => record[0] == '5'))
        {
            var serviceClassCode = record.Substring(1, 3);
            var companyEntryDescription = record.Substring(53, 10).Trim();
            var descriptiveDate = record.Substring(63, 8).Trim();

            if (serviceClassCode == "220" && string.IsNullOrWhiteSpace(descriptiveDate))
            {
                throw new InvalidOperationException("Registro tipo 5 inválido: la fecha descriptiva es obligatoria para prenotificaciones crédito y monetarias crédito.");
            }

            if (companyEntryDescription.Contains("PSE", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(companyEntryDescription, "MULTICREDI", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Registro tipo 5 inválido: las transacciones crédito originadas por PSE deben usar MULTICREDIT en la descripción del lote.");
            }
        }
    }

    private static void ValidateCenitFileHeader(IReadOnlyList<string> records, NachaValidationContext context)
    {
        if (!string.Equals(context.ClearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fileHeader = records[0];
        var immediateDestination = fileHeader.Substring(3, 10).Trim();
        if (!string.Equals(immediateDestination, context.ClearingHouse.OriginCode?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Archivo CENIT inválido: el header tipo 1 no coincide con el identificador/origen configurado para la cámara.");
        }
    }
}
