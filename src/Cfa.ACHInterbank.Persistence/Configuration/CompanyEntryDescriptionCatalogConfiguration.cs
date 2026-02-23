using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CompanyEntryDescriptionCatalogConfiguration : IEntityTypeConfiguration<CompanyEntryDescriptionCatalog>
{
    public void Configure(EntityTypeBuilder<CompanyEntryDescriptionCatalog> builder)
    {
        builder.ToTable("CompanyEntryDescriptionCatalog");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Term)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.StandardEntryClassCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.Term)
            .IsUnique();

        builder.HasData(GetSeedData());
    }

    private static IEnumerable<CompanyEntryDescriptionCatalog> GetSeedData()
    {
        string[] ppdTerms =
        [
            "ADMON", "AHORROS", "APORTES", "ARRIENDOS", "ARRIENDO", "CELULAR", "CELULARES", "CESANTIAS",
            "CLUB", "COLEGIO", "COMISIONES", "COMISION", "CONTRATIST", "DIVIDENDOS", "DONACION", "HONORARIO",
            "IMPUESTOS", "INTERESES", "NOMINA", "PROVEEDOR", "PENSIONES", "PREPAGADA", "PRESTAMOS", "RENDIMIENT",
            "RIESGOS P", "SEGUROS", "SEGURO", "SERV PUBLI", "SUSCRIPCI", "TARCREDITO", "TRASLADO", "TRASLADOS",
            "TV X CABL", "TV SATELIT", "UNIVERSIDA", "OTROS"
        ];

        string[] ccdTerms =
        [
            "PAGOS PSE", "MULTICREDIT", "COBROS PSE", "PAGOS DIAN", "SSS", "COBROS SSS"
        ];

        var seed = new List<CompanyEntryDescriptionCatalog>();
        var id = 1;

        foreach (var term in ppdTerms)
        {
            seed.Add(new CompanyEntryDescriptionCatalog
            {
                Id = id++,
                Term = term,
                StandardEntryClassCode = "PPD",
                IsActive = true
            });
        }

        foreach (var term in ccdTerms)
        {
            seed.Add(new CompanyEntryDescriptionCatalog
            {
                Id = id++,
                Term = term,
                StandardEntryClassCode = "CCD",
                IsActive = true
            });
        }

        return seed;
    }
}
