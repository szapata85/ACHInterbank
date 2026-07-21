using Cfa.ACHInterbank.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ChangedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ChangedAt)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(128);

        builder.Property(x => x.ChangedFields);

        builder.Property(x => x.BeforeJson);

        builder.Property(x => x.AfterJson);
    }
}
