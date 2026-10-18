using Cfa.ACHInterbank.Application.Security.Dtos;

namespace Cfa.ACHInterbank.Application.Security.Interfaces;

public interface ISoapIntegrationSettingsService
{
    Task<SoapIntegrationSettingsDto> GetAsync(CancellationToken ct = default);
    Task<SoapIntegrationSettingsDto> GetAdministrationAsync(CancellationToken ct = default);
    Task<SoapIntegrationSettingsChangePreviewDto> PreviewChangeAsync(
        SoapIntegrationSettingsChangePreviewRequestDto request,
        CancellationToken ct = default);
    Task<SoapIntegrationSettingsDto> SaveAsync(SoapIntegrationSettingsDto request, CancellationToken ct = default);
}
