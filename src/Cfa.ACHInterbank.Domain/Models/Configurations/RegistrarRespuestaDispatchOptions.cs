namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public sealed class RegistrarRespuestaDispatchOptions
{
    public const string SectionName = "RegistrarRespuestaTransaccion";

    public bool UseContractDrivenRequest { get; set; }
}
