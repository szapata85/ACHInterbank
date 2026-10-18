using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase;

internal static class IntegrationMappingSetRevisionGuard
{
    public static void Prepare(AchDbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<IntegrationMappingSet>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Revision < 1)
                {
                    entry.Entity.Revision = 1;
                }

                continue;
            }

            var hasAggregateChange = entry.Properties.Any(property =>
                property.IsModified
                && property.Metadata.Name is not nameof(IntegrationMappingSet.CreatedAt)
                && property.Metadata.Name is not nameof(IntegrationMappingSet.UpdatedAt)
                && property.Metadata.Name is not nameof(IntegrationMappingSet.Revision));

            var revision = entry.Property(x => x.Revision);
            var expected = checked(revision.OriginalValue + 1);
            if (!hasAggregateChange)
            {
                if (revision.IsModified && revision.CurrentValue != expected)
                {
                    throw new InvalidOperationException("La revisión del MappingSet debe avanzar exactamente una unidad.");
                }

                continue;
            }

            if (revision.CurrentValue == revision.OriginalValue)
            {
                revision.CurrentValue = expected;
                revision.IsModified = true;
            }
            else if (revision.CurrentValue != expected)
            {
                throw new InvalidOperationException("La revisión del MappingSet debe avanzar exactamente una unidad por cambio persistido.");
            }
        }
    }
}
