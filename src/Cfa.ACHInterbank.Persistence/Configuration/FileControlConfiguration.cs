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

        //builder.HasOne(x => x.NachaHeader)
        //       .WithMany()
        //       .HasForeignKey(x => x.NachaHeaderId)
        //       .OnDelete(DeleteBehavior.Cascade);
    }
}
