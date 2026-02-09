using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class CustomerAccountConfiguration : IEntityTypeConfiguration<CustomerAccount>
{
    public void Configure(EntityTypeBuilder<CustomerAccount> builder)
    {
        builder.ToTable("CustomerAccounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => new { x.CustomerId, x.AccountNumber }).IsUnique();

        builder.HasOne(x => x.Customer)
            .WithMany(c => c.Accounts)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
