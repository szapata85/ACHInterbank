using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Entities.User;

public class SoapIntegrationSetting : AuditableEntity
{
    public int Id { get; set; }
    public string WscfaachMappingsJson { get; set; } = "[]";
    public string WsAxonRespuestaTransaccionesMappingsJson { get; set; } = "[]";
}
