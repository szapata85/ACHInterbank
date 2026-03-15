namespace Cfa.ACHInterbank.Application.Helpers.ACH;

public static class BatchHeaderType5JulianDateValidator
{
    public const int RecordLength = 106;
    public const int JulianDateStartIndex = 79;
    public const int JulianDateLength = 3;
    public const string FatalError65 = "65";

    public static JulianDateValidationResult ValidateAndFormat(string? julianDate)
    {
        if (string.IsNullOrWhiteSpace(julianDate))
        {
            return JulianDateValidationResult.Success(new string(' ', JulianDateLength));
        }

        var trimmedValue = julianDate.Trim();

        if (!trimmedValue.All(char.IsDigit))
        {
            return JulianDateValidationResult.Failure(
                FatalError65,
                "Error Fatal 65: la Fecha de Compensación Juliana (posiciones 80-82) solo admite caracteres numéricos cuando está diligenciada.");
        }

        if (trimmedValue.Length > JulianDateLength)
        {
            return JulianDateValidationResult.Failure(
                "ACH-T5-JULIAN-LENGTH",
                "La Fecha de Compensación Juliana debe ocupar máximo 3 posiciones (80-82)."
            );
        }

        var julianDay = int.Parse(trimmedValue);
        if (julianDay is < 1 or > 366)
        {
            return JulianDateValidationResult.Failure(
                "ACH-T5-JULIAN-RANGE",
                "La Fecha de Compensación Juliana debe estar entre 001 y 366."
            );
        }

        return JulianDateValidationResult.Success(trimmedValue.PadLeft(JulianDateLength, '0'));
    }

    public static BatchHeaderValidationResult ApplyToType5Record(string record, string? julianDate)
    {
        if (string.IsNullOrEmpty(record))
        {
            return BatchHeaderValidationResult.Failure("ACH-T5-EMPTY", "El Registro Tipo 5 no puede ser vacío.");
        }

        if (record.Length != RecordLength)
        {
            return BatchHeaderValidationResult.Failure(
                "ACH-T5-LENGTH",
                $"El Registro Tipo 5 debe tener longitud fija de {RecordLength} caracteres."
            );
        }

        if (record[0] != '5')
        {
            return BatchHeaderValidationResult.Failure(
                "ACH-T5-ID",
                "El Registro Tipo 5 debe iniciar con el identificador fijo '5' en la posición 1."
            );
        }

        var julianValidation = ValidateAndFormat(julianDate);
        if (!julianValidation.IsValid)
        {
            return BatchHeaderValidationResult.Failure(julianValidation.ErrorCode!, julianValidation.ErrorMessage!);
        }

        var updatedRecord = record.Remove(JulianDateStartIndex, JulianDateLength)
            .Insert(JulianDateStartIndex, julianValidation.FormattedValue);

        return BatchHeaderValidationResult.Success(updatedRecord);
    }
}

public sealed record JulianDateValidationResult(bool IsValid, string FormattedValue, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static JulianDateValidationResult Success(string formattedValue) => new(true, formattedValue);

    public static JulianDateValidationResult Failure(string errorCode, string errorMessage) =>
        new(false, string.Empty, errorCode, errorMessage);
}

public sealed record BatchHeaderValidationResult(bool IsValid, string? Record = null, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static BatchHeaderValidationResult Success(string record) => new(true, record);

    public static BatchHeaderValidationResult Failure(string errorCode, string errorMessage) =>
        new(false, null, errorCode, errorMessage);
}
