using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration.SchedulerTask;

public sealed class TaskDefinitionConfiguration : IEntityTypeConfiguration<TaskDefinition>
{
    public void Configure(EntityTypeBuilder<TaskDefinition> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(100);
        builder.Property(x => x.SchedulerSynchronizationStatus).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LastSchedulerSynchronizationError).HasMaxLength(2000);
    }
}
