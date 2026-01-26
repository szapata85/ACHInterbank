namespace Cfa.ACHInterbank.Application.Branding.Dtos;

public record BrandingSettingsDto
{
    public string? PublicLogo { get; init; }
    public string? PrivateLogo { get; init; }
    public string? PublicBackground { get; init; }
    public string? PrivateBackground { get; init; }
    public string? SidebarBackground { get; init; }
    public string? ButtonColor { get; init; }
}
