using Cfa.ACHInterbank.Application.Branding.Dtos;
using Cfa.ACHInterbank.Application.Branding.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Branding;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Branding.Services;

[Scoped]
public class BrandingSettingsService : IBrandingSettingsService
{
    private readonly AchDbContext _dbContext;

    public BrandingSettingsService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BrandingSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var branding = await _dbContext.BrandingSettings
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .FirstOrDefaultAsync(ct);

        return branding is null ? new BrandingSettingsDto() : MapToDto(branding);
    }

    public async Task<BrandingSettingsDto> SaveAsync(BrandingSettingsDto request, CancellationToken ct = default)
    {
        var branding = await _dbContext.BrandingSettings
            .OrderBy(b => b.Id)
            .FirstOrDefaultAsync(ct);

        if (branding is null)
        {
            branding = new BrandingSetting();
            _dbContext.BrandingSettings.Add(branding);
        }

        branding.PublicLogo = request.PublicLogo;
        branding.PrivateLogo = request.PrivateLogo;
        branding.PublicBackground = request.PublicBackground;
        branding.PrivateBackground = request.PrivateBackground;
        branding.SidebarBackground = request.SidebarBackground;
        branding.ButtonColor = request.ButtonColor;

        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(branding);
    }

    private static BrandingSettingsDto MapToDto(BrandingSetting entity) => new()
    {
        PublicLogo = entity.PublicLogo,
        PrivateLogo = entity.PrivateLogo,
        PublicBackground = entity.PublicBackground,
        PrivateBackground = entity.PrivateBackground,
        SidebarBackground = entity.SidebarBackground,
        ButtonColor = entity.ButtonColor
    };
}
