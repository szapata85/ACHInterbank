using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Configuration
{
    public class BatchHeaderConfiguration : IEntityTypeConfiguration<BatchHeader>
    {
        public void Configure(EntityTypeBuilder<BatchHeader> builder)
        {
            builder.ToTable("BatchHeaders");
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.NachaHeader)
                   .WithMany(x => x.Batches)
                   .HasForeignKey(x => x.NachaHeaderId);
        }
    }
}
