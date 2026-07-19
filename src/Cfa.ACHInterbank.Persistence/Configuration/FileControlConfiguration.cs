using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class FileControlConfiguration : IEntityTypeConfiguration<FileControl>
{
    public void Configure(EntityTypeBuilder<FileControl> builder)
    {
        builder.ToTable("FileControls");

        builder.HasKey(x => x.FileControlID);

        builder.Property(p => p.TotalDebitAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.TotalCreditAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Reserved)
            .HasMaxLength(39);


        //builder.HasOne(x => x.NachaHeader)
        //       .WithMany()
        //       .HasForeignKey(x => x.NachaHeaderId)
        //       .OnDelete(DeleteBehavior.Cascade);
    }
}
