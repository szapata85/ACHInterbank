namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Contrato interno tipado exacto para Proc_Contrapartidas.
/// </summary>
public sealed record ProcContrapartidasRequestContract
{
    public required string OFNIT { get; init; }
    public required string OFEMP { get; init; }
    public required string OFCTA { get; init; }
    public required string OFDD { get; init; }
    public required string OFFECHEFEC { get; init; }
    public required decimal OFMONDEB { get; init; }
    public required decimal OFMONCRE { get; init; }
    public required int OFIDARCH { get; init; }
    public required int OFIDLOT { get; init; }
    public required string OFST { get; init; }
    public required string OFIDTX { get; init; }
    public required int OFIDREVER { get; init; }
    public required int OFIDEBAPLI { get; init; }
    public required int OFIDCAMCOMPE { get; init; }
    public required string OFDIRECCIONIP { get; init; }
    public required string OFLIBRE { get; init; }
    public required int OFLIBRE1 { get; init; }
    public required int ANSIDLOTE { get; init; }
    public required string ANSST { get; init; }
    public required string ANCLC { get; init; }
    public required string ANSIDTX { get; init; }
    public required int ANSIDREVER { get; init; }
}
