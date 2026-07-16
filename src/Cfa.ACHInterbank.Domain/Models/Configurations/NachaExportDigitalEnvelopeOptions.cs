namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public sealed class NachaExportDigitalEnvelopeOptions
{
    public const string SectionName = "DigitalEnvelope:NachaExport";

    public string Environment { get; set; } = "Test";
    public string RecipientPurpose { get; set; } = "OutboundEncryption";
    public string RecipientHolderType { get; set; } = "ClearingHouse";
    public bool AllowDefaultClearingHouseFallback { get; set; }
    public int DefaultClearingHouseId { get; set; } = 1;
}
