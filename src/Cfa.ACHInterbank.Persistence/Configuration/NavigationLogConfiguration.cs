using Cfa.ACHInterbank.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class NavigationLogConfiguration : IEntityTypeConfiguration<NavigationLog>
{
    public void Configure(EntityTypeBuilder<NavigationLog> builder)
    {
        builder.ToTable("NavigationLog");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.UserId)
            .HasMaxLength(200);

        builder.Property(x => x.Route)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.SessionId)
            .HasMaxLength(100);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(100);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(512);

        builder.HasIndex(x => x.VisitedAt);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Route);
    }
}
