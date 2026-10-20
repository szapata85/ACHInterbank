using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Services;

namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

/// <summary>Base de entidades que conservan marcas de creación y actualización persistidas.</summary>
public abstract class AuditableEntity : IAuditableEntity
{
    /// <summary>Fecha y hora de creación persistida de la entidad.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Fecha y hora de última actualización persistida de la entidad.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

