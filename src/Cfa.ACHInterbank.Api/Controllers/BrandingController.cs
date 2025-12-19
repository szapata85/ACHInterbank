using Cfa.ACHInterbank.Domain.Entities.Branding;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/users/branding")]
public class BrandingController : ControllerBase
{
    private readonly AchDbContext _dbContext;

    public BrandingController(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<BrandingSettingsDto>> GetBrandingAsync(CancellationToken cancellationToken)
    {
        var branding = await _dbContext.BrandingSettings
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (branding is null)
        {
            return Ok(new BrandingSettingsDto());
        }

        return Ok(MapToDto(branding));
    }

    [HttpPut]
    // El sitio público también consume la identidad visual y esta instancia no usa JWT,
    // por lo que el endpoint debe estar disponible sin autenticación.
    [AllowAnonymous]
    public async Task<ActionResult<BrandingSettingsDto>> SaveBrandingAsync(
        [FromBody] BrandingSettingsDto request,
        CancellationToken cancellationToken)
    {
        var branding = await _dbContext.BrandingSettings
            .OrderBy(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);

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

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(branding));
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

public record BrandingSettingsDto
{
    public string? PublicLogo { get; init; }
    public string? PrivateLogo { get; init; }
    public string? PublicBackground { get; init; }
    public string? PrivateBackground { get; init; }
    public string? SidebarBackground { get; init; }
    public string? ButtonColor { get; init; }
}
