using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class CustomerThirdPartyConfiguration : IEntityTypeConfiguration<CustomerThirdParty>
{
    public void Configure(EntityTypeBuilder<CustomerThirdParty> builder)
    {
        builder.ToTable("CustomerThirdParties");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.DestinationAccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(t => t.RecipientIdNumber).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsConcurrencyToken()
            .IsRequired();
        builder.Property(t => t.ValidationMessage).HasMaxLength(200);

        builder.HasOne(t => t.Customer)
            .WithMany(c => c.ThirdParties)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DestinationInstitution)
            .WithMany()
            .HasForeignKey(t => t.DestinationInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.PrenotificationTransaction)
            .WithMany()
            .HasForeignKey(t => t.PrenotificationTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => new
        {
            t.CustomerId,
            t.DestinationInstitutionId,
            t.DestinationAccountNumber,
            t.RecipientIdNumber
        }).IsUnique();
    }
}
