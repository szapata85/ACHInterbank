using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class NachaFileIdentifierMapConfiguration : IEntityTypeConfiguration<NachaFileIdentifierMap>
{
    public void Configure(EntityTypeBuilder<NachaFileIdentifierMap> builder)
    {
        builder.ToTable("NachaFileIdentifierMap");

        builder.HasKey(map => map.Id);

        builder.Property(map => map.Sequence)
            .IsRequired();

        builder.Property(map => map.Identifier)
            .HasMaxLength(1)
            .IsRequired();

        builder.HasIndex(map => map.Sequence)
            .IsUnique();

        builder.HasData(GetSeedData());
    }

    private static IEnumerable<NachaFileIdentifierMap> GetSeedData()
    {
        var seed = new List<NachaFileIdentifierMap>();

        for (int sequence = 1; sequence <= 26; sequence++)
        {
            seed.Add(new NachaFileIdentifierMap
            {
                Id = sequence,
                Sequence = sequence,
                Identifier = ((char)('A' + (sequence - 1))).ToString()
            });
        }

        for (int sequence = 27; sequence <= 36; sequence++)
        {
            seed.Add(new NachaFileIdentifierMap
            {
                Id = sequence,
                Sequence = sequence,
                Identifier = ((char)('0' + (sequence - 27))).ToString()
            });
        }

        return seed;
    }
}
