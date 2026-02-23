namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class NachaFileIdentifierMap
{
    public int Id { get; set; }
    public int Sequence { get; set; }
    public string Identifier { get; set; } = string.Empty;
}
