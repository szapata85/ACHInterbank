using Cfa.ACHInterbank.Application.Branding.Dtos;

namespace Cfa.ACHInterbank.Application.Branding.Interfaces;

public interface IBrandingSettingsService
{
    Task<BrandingSettingsDto> GetAsync(CancellationToken ct = default);
    Task<BrandingSettingsDto> SaveAsync(BrandingSettingsDto request, CancellationToken ct = default);
}
