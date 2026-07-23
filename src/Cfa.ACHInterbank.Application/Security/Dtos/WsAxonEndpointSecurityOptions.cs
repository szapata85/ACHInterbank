namespace Cfa.ACHInterbank.Application.Security.Dtos;

public sealed class WsAxonEndpointSecurityOptions
{
    public const string SectionName = "SoapEndpointSecurity:WsAxonRespuestaTransacciones";

    public WsAxonEndpointSecurityMode Mode { get; set; } = WsAxonEndpointSecurityMode.Unconfigured;
    public List<string> AllowedSchemes { get; set; } = [];
    public List<string> AllowedHosts { get; set; } = [];
    public List<int> AllowedPorts { get; set; } = [];
    public List<string> AllowedPaths { get; set; } = [];
    public bool RequireHttps { get; set; }
}

public enum WsAxonEndpointSecurityMode
{
    Unconfigured = 0,
    ControlledLocal = 1,
    ConfiguredAllowlist = 2
}
