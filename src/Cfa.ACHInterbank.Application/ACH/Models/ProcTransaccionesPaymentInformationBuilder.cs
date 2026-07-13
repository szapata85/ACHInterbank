namespace Cfa.ACHInterbank.Application.ACH.Models;

public static class ProcTransaccionesPaymentInformationBuilder
{
    public const int OutputLength = 77;

    public static string Build(
        string originatorIdentification,
        string operationDescription,
        string paymentRelatedInformation)
    {
        var originator = NormalizeRequired(originatorIdentification, 15, "IDORIG", padLeftWithZeros: true);
        var description = NormalizeRequired(operationDescription, 10, "descripción", padLeftWithZeros: false);
        var payment = paymentRelatedInformation ?? string.Empty;
        if (payment.Length != 80)
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_PAYMENT_INFORMATION_INVALID: PaymentRelatedInformation debe conservar exactamente 80 posiciones.");
        }

        var firstSegment = payment[..24];
        var complementarySegment = payment.Substring(24, 24);
        if (string.IsNullOrWhiteSpace(firstSegment) || string.IsNullOrWhiteSpace(complementarySegment))
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_PAYMENT_INFORMATION_INCOMPLETE: la addenda 05 no contiene los dos segmentos de pago requeridos.");
        }

        var result = $"{originator}  {description}{firstSegment}  {complementarySegment}";
        if (result.Length != OutputLength)
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_PAYMENT_INFORMATION_LENGTH: DESTRAN debe contener exactamente 77 caracteres.");
        }

        return result;
    }

    private static string NormalizeRequired(string value, int length, string field, bool padLeftWithZeros)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"PROC_TRANSACCIONES_PAYMENT_INFORMATION_MISSING: {field} es obligatorio.");
        }

        if (normalized.Length > length)
        {
            throw new InvalidOperationException($"PROC_TRANSACCIONES_PAYMENT_INFORMATION_OVERFLOW: {field} excede {length} posiciones y no será truncado.");
        }

        return padLeftWithZeros ? normalized.PadLeft(length, '0') : normalized.PadRight(length, ' ');
    }
}
