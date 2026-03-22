namespace Cfa.ACHInterbank.Domain.Helpers;

public static class DigitoChequeoHelper
{
    public static string CalcularDigitoChequeo(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || ruta.Length != 8)
            throw new ArgumentException("La ruta debe tener exactamente 8 dígitos.");

        if (ruta.Any(c => !char.IsDigit(c)))
            throw new ArgumentException("La ruta debe contener solo caracteres numéricos.");

        int[] pesos = { 3, 7, 1, 3, 7, 1, 3, 7 };
        int suma = 0;

        for (int i = 0; i < 8; i++)
        {
            int digito = ruta[i] - '0';
            suma += digito * pesos[i];
        }

        int proximoMultiplo10 = ((suma + 9) / 10) * 10;
        int digitoChequeo = proximoMultiplo10 - suma;

        return (digitoChequeo == 10 ? 0 : digitoChequeo).ToString();
    }
}
