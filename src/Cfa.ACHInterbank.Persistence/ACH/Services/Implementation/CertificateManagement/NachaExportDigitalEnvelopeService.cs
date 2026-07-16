using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

[Scoped]
public sealed class NachaExportDigitalEnvelopeService : INachaExportDigitalEnvelopeService
{
    private readonly ICertificateSelectionService _certificateSelection;
    private readonly IManagedDigitalEnvelopeService _envelopeService;
    private readonly NachaExportDigitalEnvelopeOptions _options;

    public NachaExportDigitalEnvelopeService(
        ICertificateSelectionService certificateSelection,
        IManagedDigitalEnvelopeService envelopeService,
        IOptions<NachaExportDigitalEnvelopeOptions> options)
    {
        _certificateSelection = certificateSelection;
        _envelopeService = envelopeService;
        _options = options.Value;
    }

    public async Task<ManagedDigitalEnvelopeResult> EncryptAsync(
        int clearingHouseId,
        string fileName,
        byte[] content,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var environment = Parse<CertificateEnvironment>(_options.Environment, "Environment");
        var purpose = Parse<CertificatePurpose>(_options.RecipientPurpose, "RecipientPurpose");
        var holderType = Parse<CertificateHolderType>(_options.RecipientHolderType, "RecipientHolderType");

        if (purpose is not (CertificatePurpose.OutboundEncryption or CertificatePurpose.InboundDecryption))
        {
            throw new ManagedDigitalEnvelopeException(
                "CERTIFICATE_PURPOSE_INVALID",
                "El propósito configurado para el destinatario del sobre no permite cifrado.");
        }

        var selected = await _certificateSelection.SelectActiveAsync(
            clearingHouseId,
            environment,
            purpose,
            holderType,
            cancellationToken);

        if (selected is null
            && _options.AllowDefaultClearingHouseFallback
            && _options.DefaultClearingHouseId > 0
            && _options.DefaultClearingHouseId != clearingHouseId)
        {
            selected = await _certificateSelection.SelectActiveAsync(
                _options.DefaultClearingHouseId,
                environment,
                purpose,
                holderType,
                cancellationToken);
        }

        if (selected is null)
        {
            throw new ManagedDigitalEnvelopeException(
                "CERTIFICATE_NOT_FOUND",
                "No existe un certificado activo y vigente para cifrar la exportación NACHA-M.");
        }

        return await _envelopeService.EncryptAsync(
            new ManagedDigitalEnvelopeRequest(selected.Id, fileName, content, actor),
            cancellationToken);
    }

    private static T Parse<T>(string configuredValue, string optionName) where T : struct, Enum
    {
        if (Enum.TryParse<T>(configuredValue, true, out var value))
        {
            return value;
        }

        throw new ManagedDigitalEnvelopeException(
            "DIGITAL_ENVELOPE_CONFIGURATION_INVALID",
            $"La opción DigitalEnvelope:NachaExport:{optionName} no es válida.");
    }
}
