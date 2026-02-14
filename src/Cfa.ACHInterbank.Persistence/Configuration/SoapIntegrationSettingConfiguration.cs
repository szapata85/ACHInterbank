using Cfa.ACHInterbank.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class SoapIntegrationSettingConfiguration : IEntityTypeConfiguration<SoapIntegrationSetting>
{
    public void Configure(EntityTypeBuilder<SoapIntegrationSetting> builder)
    {
        builder.ToTable("SoapIntegrationSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WscfaachMappingsJson)
            .IsRequired();

        builder.Property(x => x.WsAxonRespuestaTransaccionesMappingsJson)
            .IsRequired();
    }
}
