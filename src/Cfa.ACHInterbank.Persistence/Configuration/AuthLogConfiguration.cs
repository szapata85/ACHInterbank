using Cfa.ACHInterbank.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AuthLogConfiguration : IEntityTypeConfiguration<AuthLog>
{
    public void Configure(EntityTypeBuilder<AuthLog> builder)
    {
        builder.ToTable("AuthLog");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Username)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(400);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(100);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(512);

        builder.HasIndex(x => x.LoggedAt);
    }
}
