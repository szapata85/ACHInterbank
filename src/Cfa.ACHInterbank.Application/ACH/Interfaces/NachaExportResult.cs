namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public sealed record NachaExportResult(
    byte[] Content,
    string ContentType,
    string FileName,
    bool IsEncrypted);
